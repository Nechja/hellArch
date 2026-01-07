namespace HellArch.DataTypes.Identity

open System

/// OAuth 2.0 authorization code grant (short-lived, 5 minutes).
type AuthorizationCode = {
    Code: string
    ClientId: string
    UserId: Guid
    RedirectUri: string
    Scope: string
    CodeChallenge: string option
    CodeChallengeMethod: string option
    ExpiresAt: DateTime
    CreatedAt: DateTime
}

/// OAuth 2.0 refresh token (long-lived, 30 days).
and RefreshToken = {
    Token: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
}

/// OAuth 2.0 access token (short-lived, 1 hour).
/// Stored for validation until expiry.
and AccessToken = {
    Token: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
}

/// OAuth 2.0 client application registration.
and OAuthClient = {
    Id: string
    SecretHash: string
    Name: string
    RedirectUris: string list
    IsActive: bool
    CreatedAt: DateTime
}

/// Local identity user with username/password authentication.
and IdentityUser = {
    Id: Guid
    Email: string
    Username: string
    PasswordHash: string
    IsActive: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

