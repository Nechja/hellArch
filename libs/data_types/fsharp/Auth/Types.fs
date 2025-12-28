namespace HellArch.DataTypes.Auth

open System

/// Core user identity in the auth system.
type User = {
    Id: Guid
    Email: string
    Username: string
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// User session for cookie-based authentication.
and Session = {
    Id: Guid
    UserId: Guid
    Token: string
    IpAddress: string option
    UserAgent: string option
    ExpiresAt: DateTime
    CreatedAt: DateTime
    User: User option
}

/// OAuth provider account linked to a user.
/// Stores provider-specific identity info. Token storage is optional
/// (only needed if we want to call provider APIs on behalf of user).
and LinkedAccount = {
    Id: Guid
    UserId: Guid
    Provider: string
    ProviderUserId: string
    Email: string option
    Username: string option
    AvatarUrl: string option
    AccessToken: string option
    RefreshToken: string option
    TokenExpiresAt: DateTime option
    CreatedAt: DateTime
    UpdatedAt: DateTime
    User: User option
}
