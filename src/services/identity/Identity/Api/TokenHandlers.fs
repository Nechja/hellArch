namespace Identity.Api

open System
open Microsoft.AspNetCore.Http
open Identity.Domain
open Identity.Infrastructure
open Identity.Api.HttpUtils
open Identity.Api.Config

module TokenHandlers =

    let private handleAuthorizationCodeGrant
        (config: IdentityConfig)
        (ctx: HttpContext)
        (form: IFormCollection)
        (clientId: string)
        =
        task {
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
                if authCode.ClientId <> clientId then
                    do! writeJson ctx 400 {| error = "invalid_grant" |}
                elif authCode.RedirectUri <> redirectUri then
                    do! writeJson ctx 400 {| error = "invalid_grant" |}
                elif authCode.ExpiresAt < DateTime.UtcNow then
                    do! writeJson ctx 400 {| error = "invalid_grant"; error_description = "code_expired" |}
                else
                    let pkceValid =
                        match authCode.CodeChallenge, authCode.CodeChallengeMethod, codeVerifier with
                        | Some challenge, Some method, Some verifier ->
                            TokenGenerator.verifyCodeChallenge verifier challenge method
                        | Some _, Some _, None -> false
                        | Some _, None, _ -> false
                        | None, _, _ -> false

                    if not pkceValid then
                        do! writeJson ctx 400 {| error = "invalid_grant"; error_description = "pkce_failed" |}
                    else
                        let accessToken = TokenGenerator.generateAccessToken()
                        let refreshToken = TokenGenerator.generateRefreshToken()
                        let accessExpiresAt = DateTime.UtcNow.Add(config.AccessTokenTtl)
                        let refreshExpiresAt = DateTime.UtcNow.Add(config.RefreshTokenTtl)
                        let fingerprint = ClientFingerprint.generate ctx
                        let familyId = Guid.NewGuid()

                        let! accessResult = Database.createAccessToken config.ConnectionString accessToken authCode.UserId clientId authCode.Scope accessExpiresAt fingerprint |> Async.StartAsTask
                        let! refreshResult = Database.createRefreshToken config.ConnectionString refreshToken authCode.UserId clientId authCode.Scope refreshExpiresAt familyId fingerprint |> Async.StartAsTask

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
        }

    let private handleRefreshTokenGrant
        (config: IdentityConfig)
        (ctx: HttpContext)
        (form: IFormCollection)
        (clientId: string)
        =
        task {
            let refreshToken = form.["refresh_token"].ToString()

            let! tokenResult = Database.useRefreshToken config.ConnectionString refreshToken |> Async.StartAsTask

            match tokenResult with
            | Error e ->
                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
            | Ok Database.NotFound ->
                do! writeJson ctx 401 {| error = "invalid_grant" |}
            | Ok (Database.ReuseDetected familyId) ->
                let! _ = Database.revokeTokenFamily config.ConnectionString familyId |> Async.StartAsTask
                do! writeJson ctx 401 {| error = "invalid_grant"; error_description = "token_reuse_detected" |}
            | Ok (Database.Valid oldToken) ->
                if oldToken.ClientId <> clientId then
                    do! writeJson ctx 401 {| error = "invalid_grant" |}
                elif oldToken.ExpiresAt < DateTime.UtcNow then
                    do! writeJson ctx 401 {| error = "invalid_grant"; error_description = "token_expired" |}
                else
                    let currentFingerprint = ClientFingerprint.generate ctx
                    let fingerprintValid =
                        match oldToken.ClientFingerprint, currentFingerprint with
                        | Some stored, Some current -> stored = current
                        | None, _ -> true
                        | Some _, None -> false

                    if not fingerprintValid then
                        let! _ = Database.revokeTokenFamily config.ConnectionString oldToken.FamilyId |> Async.StartAsTask
                        do! writeJson ctx 401 {| error = "invalid_grant"; error_description = "client_mismatch" |}
                    else
                        let newAccessToken = TokenGenerator.generateAccessToken()
                        let newRefreshToken = TokenGenerator.generateRefreshToken()
                        let accessExpiresAt = DateTime.UtcNow.Add(config.AccessTokenTtl)
                        let refreshExpiresAt = DateTime.UtcNow.Add(config.RefreshTokenTtl)
                        let fingerprint = ClientFingerprint.generate ctx

                        let! accessResult = Database.createAccessToken config.ConnectionString newAccessToken oldToken.UserId clientId oldToken.Scope accessExpiresAt fingerprint |> Async.StartAsTask
                        let! refreshResult = Database.createRefreshToken config.ConnectionString newRefreshToken oldToken.UserId clientId oldToken.Scope refreshExpiresAt oldToken.FamilyId fingerprint |> Async.StartAsTask

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
        }

    let handleToken (config: IdentityConfig) (ctx: HttpContext) =
        task {
            let! form = ctx.Request.ReadFormAsync()

            let grantType = form.["grant_type"].ToString()
            let clientId = form.["client_id"].ToString()
            let clientSecret = form.["client_secret"].ToString()

            let! clientResult = Database.getClient config.ConnectionString clientId |> Async.StartAsTask

            match clientResult with
            | Error e ->
                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
            | Ok None ->
                do! writeJson ctx 401 {| error = "invalid_client" |}
            | Ok (Some client) ->
                if not (PasswordHasher.verify clientSecret client.SecretHash) then
                    do! writeJson ctx 401 {| error = "invalid_client" |}
                else
                    match grantType with
                    | "authorization_code" ->
                        do! handleAuthorizationCodeGrant config ctx form clientId
                    | "refresh_token" ->
                        do! handleRefreshTokenGrant config ctx form clientId
                    | _ ->
                        do! writeJson ctx 400 {| error = "unsupported_grant_type" |}
        }

    let handleRevoke (config: IdentityConfig) (ctx: HttpContext) =
        task {
            let! form = ctx.Request.ReadFormAsync()

            let token = form.["token"].ToString()
            let tokenTypeHint = if form.ContainsKey("token_type_hint") then Some (form.["token_type_hint"].ToString()) else None

            if String.IsNullOrWhiteSpace(token) then
                do! writeJson ctx 400 {| error = "invalid_request"; error_description = "token is required" |}
            else
                let! result =
                    match tokenTypeHint with
                    | Some "access_token" ->
                        Database.revokeAccessToken config.ConnectionString token |> Async.StartAsTask
                    | Some "refresh_token" ->
                        Database.revokeRefreshToken config.ConnectionString token |> Async.StartAsTask
                    | _ ->
                        task {
                            let! refreshResult = Database.revokeRefreshToken config.ConnectionString token |> Async.StartAsTask
                            match refreshResult with
                            | Ok true -> return Ok true
                            | Ok false ->
                                let! accessResult = Database.revokeAccessToken config.ConnectionString token |> Async.StartAsTask
                                return accessResult
                            | Error e -> return Error e
                        }

                match result with
                | Error e ->
                    do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                | Ok _ ->
                    ctx.Response.StatusCode <- 200
        }
