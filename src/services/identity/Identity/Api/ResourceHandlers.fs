namespace Identity.Api

open System
open Microsoft.AspNetCore.Http
open Identity.Domain
open Identity.Infrastructure
open Identity.Api.HttpUtils
open Identity.Api.Config

module ResourceHandlers =

    let handleUserInfo (config: IdentityConfig) (ctx: HttpContext) =
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
                                Sub = user.Id.ToString()
                                Email = user.Email
                                Username = user.Username
                                AvatarUrl = None
                            }
                            do! writeJson ctx 200 response
        }
