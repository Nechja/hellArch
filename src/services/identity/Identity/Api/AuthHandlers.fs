namespace Identity.Api

open System
open System.Text.Json
open Microsoft.AspNetCore.Http
open Identity.Domain
open Identity.Infrastructure
open Identity.Api.HttpUtils
open Identity.Api.Config

module AuthHandlers =

    let handleAuthorize (config: IdentityConfig) (ctx: HttpContext) =
        task {
            let query = ctx.Request.Query

            let request: AuthorizeRequest = {
                ResponseType = query.["response_type"].ToString()
                ClientId = query.["client_id"].ToString()
                RedirectUri = query.["redirect_uri"].ToString()
                Scope = if query.ContainsKey("scope") then Some (query.["scope"].ToString()) else None
                State = query.["state"].ToString()
                CodeChallenge = if query.ContainsKey("code_challenge") then Some (query.["code_challenge"].ToString()) else None
                CodeChallengeMethod = if query.ContainsKey("code_challenge_method") then Some (query.["code_challenge_method"].ToString()) else None
            }

            if request.ResponseType <> "code" then
                do! writeJson ctx 400 {| error = "unsupported_response_type" |}
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
                        let sessionId = TokenGenerator.generateToken 32
                        let session: AuthorizationSession = { Request = request; CreatedAt = DateTime.UtcNow }
                        SessionStore.add sessionId session

                        let loginUrl = Views.loginUrl config.ClientBaseUrl sessionId
                        ctx.Response.Redirect(loginUrl)
        }

    let handleLogin (config: IdentityConfig) (ctx: HttpContext) =
        task {
            let! body = JsonSerializer.DeserializeAsync<LoginBody>(ctx.Request.Body, jsonOptions)

            match SessionStore.tryGet body.Session with
            | None ->
                do! writeJson ctx 400 {| error = "invalid_session" |}
            | Some session ->
                if DateTime.UtcNow - session.CreatedAt > TimeSpan.FromMinutes(10.0) then
                    SessionStore.tryRemove body.Session
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
                            SessionStore.tryRemove body.Session
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

    let handleRegister (config: IdentityConfig) (ctx: HttpContext) =
        task {
            let! body = JsonSerializer.DeserializeAsync<RegisterBody>(ctx.Request.Body, jsonOptions)

            match SessionStore.tryGet body.Session with
            | None ->
                do! writeJson ctx 400 {| error = "invalid_session" |}
            | Some session ->
                if DateTime.UtcNow - session.CreatedAt > TimeSpan.FromMinutes(10.0) then
                    do! writeJson ctx 400 {| error = "session_expired" |}
                else
                    let regRequest: RegistrationRequest = { Email = body.Email; Username = body.Username; Password = body.Password }

                    match Validation.validateRegistration regRequest with
                    | Error e ->
                        do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                    | Ok validated ->
                        let! breachResult = BreachDetection.validateNotBreached validated.Password |> Async.StartAsTask
                        match breachResult with
                        | Error msg ->
                            do! writeJson ctx 400 {| error = "weak_password"; error_description = msg |}
                        | Ok () ->
                            let passwordHash = PasswordHasher.hash validated.Password
                            let! createResult = Database.createUser config.ConnectionString validated.Email validated.Username passwordHash |> Async.StartAsTask

                            match createResult with
                            | Error e ->
                                do! writeJson ctx (errorToStatus e) {| error = errorToMessage e |}
                            | Ok userId ->
                                SessionStore.tryRemove body.Session

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
