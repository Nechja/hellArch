namespace Identity.Domain

open System

/// Service-level error types following Railway Oriented Programming
[<RequireQualifiedAccess>]
type IdentityError =
    | NotFound of message: string
    | ValidationError of message: string
    | DatabaseError of message: string
    | InvalidCredentials
    | UserExists of email: string
    | InvalidAuthorizationCode
    | InvalidClient
    | InvalidRedirectUri
    | ExpiredCode
    | InvalidRefreshToken
    | InvalidAccessToken
    | InvalidPkce

/// Result type alias for identity operations
type IdentityResult<'T> = Result<'T, IdentityError>

/// OAuth 2.0 Authorization Request
type AuthorizeRequest = {
    ResponseType: string
    ClientId: string
    RedirectUri: string
    Scope: string option
    State: string
    CodeChallenge: string option
    CodeChallengeMethod: string option
}

/// OAuth 2.0 Token Request
type TokenRequest = {
    GrantType: string
    Code: string option
    RedirectUri: string option
    ClientId: string
    ClientSecret: string
    CodeVerifier: string option
    RefreshToken: string option
}

/// User registration request
type RegistrationRequest = {
    Email: string
    Username: string
    Password: string
}

/// User login request
type LoginRequest = {
    Email: string
    Password: string
}

/// OAuth 2.0 Token Response
type TokenResponse = {
    AccessToken: string
    TokenType: string
    ExpiresIn: int
    RefreshToken: string option
    Scope: string option
}

/// UserInfo endpoint response
type UserInfoResponse = {
    Id: string
    Email: string
    Username: string
    AvatarUrl: string option
}

/// Authorization session stored in memory during login flow
type AuthorizationSession = {
    Request: AuthorizeRequest
    CreatedAt: DateTime
}

/// Login request body
type LoginBody = {
    Session: string
    Email: string
    Password: string
}

/// Registration request body
type RegisterBody = {
    Session: string
    Email: string
    Username: string
    Password: string
}
