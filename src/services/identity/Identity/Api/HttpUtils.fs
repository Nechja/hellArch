namespace Identity.Api

open Microsoft.AspNetCore.Http
open Identity.Domain
open HellArch.HttpUtil

module HttpUtils =

    let jsonOptions = Json.options

    let writeJson = HttpResponse.writeJson

    let errorToStatus (error: IdentityError) =
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

    let errorToMessage (error: IdentityError) =
        match error with
        | IdentityError.NotFound msg -> msg
        | IdentityError.ValidationError msg -> msg
        | IdentityError.InvalidCredentials -> "Invalid email or password"
        | IdentityError.UserExists _ -> "Email already registered"
        | IdentityError.InvalidClient -> "Invalid client"
        | IdentityError.InvalidRedirectUri -> "Invalid redirect URI"
        | IdentityError.InvalidAuthorizationCode -> "Invalid authorization code"
        | IdentityError.ExpiredCode -> "Authorization code expired"
        | IdentityError.InvalidRefreshToken -> "Invalid refresh token"
        | IdentityError.InvalidAccessToken -> "Invalid access token"
        | IdentityError.InvalidPkce -> "Invalid PKCE code verifier"
        | IdentityError.DatabaseError msg -> $"Database error: {msg}"

    let handleError (ctx: HttpContext) (error: IdentityError) =
        writeJson ctx (errorToStatus error) {| error = errorToMessage error |}
