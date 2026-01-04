namespace Identity.Api

open System
open System.Collections.Concurrent
open System.Text.Json
open Microsoft.AspNetCore.Http
open Identity.Domain
open Identity.Infrastructure

module Handlers =

    // In-memory session storage for authorization requests
    // In production, use Redis
    let private authSessions = ConcurrentDictionary<string, AuthorizationSession>()

    let private jsonOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)

    type Config = {
        ConnectionString: string
        ClientBaseUrl: string  // e.g., "http://localhost:5000/identity"
        AccessTokenTtl: TimeSpan
        RefreshTokenTtl: TimeSpan
        AuthCodeTtl: TimeSpan
    }

    // Helper to write JSON response
    let private writeJson (ctx: HttpContext) (statusCode: int) (data: obj) =
        task {
            ctx.Response.StatusCode <- statusCode
            ctx.Response.ContentType <- "application/json"
            do! JsonSerializer.SerializeAsync(ctx.Response.Body, data, jsonOptions)
        }

    let private errorToStatus (error: IdentityError) =
        match error with
        | IdentityError.NotFound _ -> 404
        | IdentityError.ValidationError _ -> 400
        | IdentityError.InvalidCredentials -> 401
        | IdentityError.UserExists _ -> 409
        | IdentityError.InvalidClient -> 401
        | IdentityError.InvalidRedirectUri -> 400
        | IdentityError.InvalidAuthorizationCode -> 400
        | IdentityError.ExpiredCode -> 400
        | IdentityError.InvalidRefreshToken -> 401
        | IdentityError.InvalidAccessToken -> 401
        | IdentityError.InvalidPkce -> 400
        | IdentityError.DatabaseError _ -> 500

    let private errorToMessage (error: IdentityError) =
        match error with
        | IdentityError.NotFound msg -> msg
        | IdentityError.ValidationError msg -> msg
        | IdentityError.InvalidCredentials -> "Invalid email or password"
        | IdentityError.UserExists email -> $"User with email {email} already exists"
        | IdentityError.InvalidClient -> "Invalid client"
        | IdentityError.InvalidRedirectUri -> "Invalid redirect URI"
        | IdentityError.InvalidAuthorizationCode -> "Invalid authorization code"
        | IdentityError.ExpiredCode -> "Authorization code expired"
        | IdentityError.InvalidRefreshToken -> "Invalid refresh token"
        | IdentityError.InvalidAccessToken -> "Invalid access token"
        | IdentityError.InvalidPkce -> "Invalid PKCE code verifier"
        | IdentityError.DatabaseError msg -> $"Database error: {msg}"

    /// GET /identity/authorize - OAuth 2.0 Authorization Endpoint
    let handleAuthorize (config: Config) (ctx: HttpContext) =
        task {
            let query = ctx.Request.Query

            let request = {
                ResponseType = query.["response_type"].ToString()
                ClientId = query.["client_id"].ToString()
                RedirectUri = query.["redirect_uri"].ToString()
                Scope = if query.ContainsKey("scope") then Some (query.["scope"].ToString()) else None
                State = query.["state"].ToString()
                CodeChallenge = if query.ContainsKey("code_challenge") then Some (query.["code_challenge"].ToString()) else None
                CodeChallengeMethod = if query.ContainsKey("code_challenge_method") then Some (query.["code_challenge_method"].ToString()) else None
            }

            // Validate response_type
            if request.ResponseType <> "code" then
                do! writeJson ctx 400 {| error = "unsupported_response_type" |}
            // Validate client and redirect URI
            else
                let! clientResult = Database.getClient config.ConnectionString request.ClientId |> Async.StartAsTask

                match clientResult with
                | Error e ->
                    do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                | Ok None ->
                    do! writeJson ctx 401 {| error = "invalid_client" |}
                | Ok (Some client) ->
                    match Validation.validateRedirectUri client.RedirectUris request.RedirectUri with
                    | Error _ ->
                        do! writeJson ctx 400 {| error = "invalid_redirect_uri" |}
                    | Ok _ ->
                        // Create session and redirect to login
                        let sessionId = TokenGenerator.generateToken 32
                        let session = { Request = request; CreatedAt = DateTime.UtcNow }
                        authSessions.[sessionId] <- session

                        let loginUrl = Views.loginUrl config.ClientBaseUrl sessionId
                        ctx.Response.Redirect(loginUrl)
        }

    /// POST /identity/login - Handle login form submission
    let handleLogin (config: Config) (ctx: HttpContext) =
        task {
            let! body = JsonSerializer.DeserializeAsync<LoginBody>(ctx.Request.Body, jsonOptions)

            match authSessions.TryRemove(body.Session) with
            | false, _ ->
                do! writeJson ctx 400 {| error = "invalid_session" |}
            | true, session ->
                // Check session expiry (10 minutes)
                if DateTime.UtcNow - session.CreatedAt > TimeSpan.FromMinutes(10.0) then
                    do! writeJson ctx 400 {| error = "session_expired" |}
                else
                    let! userResult = Database.getUserByEmail config.ConnectionString body.Email |> Async.StartAsTask

                    match userResult with
                    | Error e ->
                        do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                    | Ok None ->
                        do! writeJson ctx 401 {| error = "invalid_credentials" |}
                    | Ok (Some user) ->
                        if not user.IsActive then
                            do! writeJson ctx 401 {| error = "account_disabled" |}
                        elif not (PasswordHasher.verify body.Password user.PasswordHash) then
                            do! writeJson ctx 401 {| error = "invalid_credentials" |}
                        else
                            // Generate authorization code
                            let code = TokenGenerator.generateAuthorizationCode()
                            let expiresAt = DateTime.UtcNow.Add(config.AuthCodeTtl)
                            let scope = session.Request.Scope |> Option.defaultValue "openid"

                            let! createResult =
                                Database.createAuthorizationCode
                                    config.ConnectionString
                                    code
                                    session.Request.ClientId
                                    user.Id
                                    session.Request.RedirectUri
                                    scope
                                    session.Request.CodeChallenge
                                    session.Request.CodeChallengeMethod
                                    expiresAt
                                |> Async.StartAsTask

                            match createResult with
                            | Error e ->
                                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                            | Ok () ->
                                let callbackUrl = Views.callbackUrl session.Request.RedirectUri code session.Request.State
                                do! writeJson ctx 200 {| redirect_url = callbackUrl |}
        }

    /// POST /identity/register - Handle registration
    let handleRegister (config: Config) (ctx: HttpContext) =
        task {
            let! body = JsonSerializer.DeserializeAsync<RegisterBody>(ctx.Request.Body, jsonOptions)

            match authSessions.TryGetValue(body.Session) with
            | false, _ ->
                do! writeJson ctx 400 {| error = "invalid_session" |}
            | true, session ->
                if DateTime.UtcNow - session.CreatedAt > TimeSpan.FromMinutes(10.0) then
                    do! writeJson ctx 400 {| error = "session_expired" |}
                else
                    let regRequest = { Email = body.Email; Username = body.Username; Password = body.Password }

                    match Validation.validateRegistration regRequest with
                    | Error e ->
                        do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                    | Ok validated ->
                        let passwordHash = PasswordHasher.hash validated.Password
                        let! createResult = Database.createUser config.ConnectionString validated.Email validated.Username passwordHash |> Async.StartAsTask

                        match createResult with
                        | Error e ->
                            do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                        | Ok userId ->
                            // Remove session from memory, generate auth code
                            authSessions.TryRemove(body.Session) |> ignore

                            let code = TokenGenerator.generateAuthorizationCode()
                            let expiresAt = DateTime.UtcNow.Add(config.AuthCodeTtl)
                            let scope = session.Request.Scope |> Option.defaultValue "openid"

                            let! codeResult =
                                Database.createAuthorizationCode
                                    config.ConnectionString
                                    code
                                    session.Request.ClientId
                                    userId
                                    session.Request.RedirectUri
                                    scope
                                    session.Request.CodeChallenge
                                    session.Request.CodeChallengeMethod
                                    expiresAt
                                |> Async.StartAsTask

                            match codeResult with
                            | Error e ->
                                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                            | Ok () ->
                                let callbackUrl = Views.callbackUrl session.Request.RedirectUri code session.Request.State
                                do! writeJson ctx 201 {| redirect_url = callbackUrl |}
        }

    /// POST /identity/token - OAuth 2.0 Token Endpoint
    let handleToken (config: Config) (ctx: HttpContext) =
        task {
            // Parse form data
            let! form = ctx.Request.ReadFormAsync()

            let grantType = form.["grant_type"].ToString()
            let clientId = form.["client_id"].ToString()
            let clientSecret = form.["client_secret"].ToString()

            // Validate client
            let! clientResult = Database.getClient config.ConnectionString clientId |> Async.StartAsTask

            match clientResult with
            | Error e ->
                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
            | Ok None ->
                do! writeJson ctx 401 {| error = "invalid_client" |}
            | Ok (Some client) ->
                // Verify client secret
                if not (PasswordHasher.verify clientSecret client.SecretHash) then
                    do! writeJson ctx 401 {| error = "invalid_client" |}
                else
                    match grantType with
                    | "authorization_code" ->
                        let code = form.["code"].ToString()
                        let redirectUri = form.["redirect_uri"].ToString()
                        let codeVerifier = if form.ContainsKey("code_verifier") then Some (form.["code_verifier"].ToString()) else None

                        let! codeResult = Database.getAndDeleteAuthorizationCode config.ConnectionString code |> Async.StartAsTask

                        match codeResult with
                        | Error e ->
                            do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                        | Ok None ->
                            do! writeJson ctx 400 {| error = "invalid_grant" |}
                        | Ok (Some authCode) ->
                            // Validate
                            if authCode.ClientId <> clientId then
                                do! writeJson ctx 400 {| error = "invalid_grant" |}
                            elif authCode.RedirectUri <> redirectUri then
                                do! writeJson ctx 400 {| error = "invalid_grant" |}
                            elif authCode.ExpiresAt < DateTime.UtcNow then
                                do! writeJson ctx 400 {| error = "invalid_grant"; error_description = "code_expired" |}
                            else
                                // Validate PKCE if present
                                let pkceValid =
                                    match authCode.CodeChallenge, authCode.CodeChallengeMethod, codeVerifier with
                                    | Some challenge, Some method, Some verifier ->
                                        TokenGenerator.verifyCodeChallenge verifier challenge method
                                    | Some _, Some _, None ->
                                        false // PKCE required but no verifier
                                    | Some _, None, _ ->
                                        false // Challenge without method is invalid
                                    | None, _, _ ->
                                        true // No PKCE required

                                if not pkceValid then
                                    do! writeJson ctx 400 {| error = "invalid_grant"; error_description = "pkce_failed" |}
                                else
                                    // Generate tokens
                                    let accessToken = TokenGenerator.generateAccessToken()
                                    let refreshToken = TokenGenerator.generateRefreshToken()
                                    let accessExpiresAt = DateTime.UtcNow.Add(config.AccessTokenTtl)
                                    let refreshExpiresAt = DateTime.UtcNow.Add(config.RefreshTokenTtl)

                                    let! accessResult = Database.createAccessToken config.ConnectionString accessToken authCode.UserId clientId authCode.Scope accessExpiresAt |> Async.StartAsTask
                                    let! refreshResult = Database.createRefreshToken config.ConnectionString refreshToken authCode.UserId clientId authCode.Scope refreshExpiresAt |> Async.StartAsTask

                                    match accessResult, refreshResult with
                                    | Ok (), Ok () ->
                                        let response: TokenResponse = {
                                            AccessToken = accessToken
                                            TokenType = "Bearer"
                                            ExpiresIn = int config.AccessTokenTtl.TotalSeconds
                                            RefreshToken = Some refreshToken
                                            Scope = Some authCode.Scope
                                        }
                                        do! writeJson ctx 200 response
                                    | _ ->
                                        do! writeJson ctx 500 {| error = "server_error" |}

                    | "refresh_token" ->
                        let refreshToken = form.["refresh_token"].ToString()

                        let! tokenResult = Database.getAndDeleteRefreshToken config.ConnectionString refreshToken |> Async.StartAsTask

                        match tokenResult with
                        | Error e ->
                            do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                        | Ok None ->
                            do! writeJson ctx 401 {| error = "invalid_grant" |}
                        | Ok (Some oldToken) ->
                            if oldToken.ClientId <> clientId then
                                do! writeJson ctx 401 {| error = "invalid_grant" |}
                            elif oldToken.ExpiresAt < DateTime.UtcNow then
                                do! writeJson ctx 401 {| error = "invalid_grant"; error_description = "token_expired" |}
                            else
                                // Generate new tokens
                                let newAccessToken = TokenGenerator.generateAccessToken()
                                let newRefreshToken = TokenGenerator.generateRefreshToken()
                                let accessExpiresAt = DateTime.UtcNow.Add(config.AccessTokenTtl)
                                let refreshExpiresAt = DateTime.UtcNow.Add(config.RefreshTokenTtl)

                                let! accessResult = Database.createAccessToken config.ConnectionString newAccessToken oldToken.UserId clientId oldToken.Scope accessExpiresAt |> Async.StartAsTask
                                let! refreshResult = Database.createRefreshToken config.ConnectionString newRefreshToken oldToken.UserId clientId oldToken.Scope refreshExpiresAt |> Async.StartAsTask

                                match accessResult, refreshResult with
                                | Ok (), Ok () ->
                                    let response: TokenResponse = {
                                        AccessToken = newAccessToken
                                        TokenType = "Bearer"
                                        ExpiresIn = int config.AccessTokenTtl.TotalSeconds
                                        RefreshToken = Some newRefreshToken
                                        Scope = Some oldToken.Scope
                                    }
                                    do! writeJson ctx 200 response
                                | _ ->
                                    do! writeJson ctx 500 {| error = "server_error" |}

                    | _ ->
                        do! writeJson ctx 400 {| error = "unsupported_grant_type" |}
        }

    /// GET /identity/userinfo - OAuth 2.0 UserInfo Endpoint
    let handleUserInfo (config: Config) (ctx: HttpContext) =
        task {
            let authHeader = ctx.Request.Headers.["Authorization"].ToString()

            if not (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) then
                ctx.Response.StatusCode <- 401
                ctx.Response.Headers.["WWW-Authenticate"] <- "Bearer"
            else
                let token = authHeader.Substring(7)
                let! tokenResult = Database.getAccessToken config.ConnectionString token |> Async.StartAsTask

                match tokenResult with
                | Error e ->
                    do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                | Ok None ->
                    ctx.Response.StatusCode <- 401
                    ctx.Response.Headers.["WWW-Authenticate"] <- "Bearer error=\"invalid_token\""
                | Ok (Some accessToken) ->
                    if accessToken.ExpiresAt < DateTime.UtcNow then
                        ctx.Response.StatusCode <- 401
                        ctx.Response.Headers.["WWW-Authenticate"] <- "Bearer error=\"invalid_token\", error_description=\"token_expired\""
                    else
                        let! userResult = Database.getUserById config.ConnectionString accessToken.UserId |> Async.StartAsTask

                        match userResult with
                        | Error e ->
                            do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                        | Ok None ->
                            do! writeJson ctx 404 {| error = "user_not_found" |}
                        | Ok (Some user) ->
                            let response: UserInfoResponse = {
                                Id = user.Id.ToString()
                                Email = user.Email
                                Username = user.Username
                                AvatarUrl = None
                            }
                            do! writeJson ctx 200 response
        }

    /// GET /identity/health
    let handleHealth (ctx: HttpContext) =
        task {
            do! writeJson ctx 200 {| status = "healthy" |}
        }
