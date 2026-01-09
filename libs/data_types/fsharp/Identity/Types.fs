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
    User: IdentityUser option
}

/// OAuth 2.0 refresh token (long-lived, 30 days).
/// Stored as SHA-256 hash for security.
/// Supports token rotation with reuse detection.
and RefreshToken = {
    TokenHash: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
    FamilyId: Guid
    IsRevoked: bool
    ClientFingerprint: string option
    User: IdentityUser option
}

/// OAuth 2.0 access token (short-lived, 1 hour).
/// Stored as SHA-256 hash for security.
/// Bound to client fingerprint to prevent token theft.
and AccessToken = {
    TokenHash: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
    ClientFingerprint: string option
    User: IdentityUser option
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
    RefreshTokens: RefreshToken list
}

