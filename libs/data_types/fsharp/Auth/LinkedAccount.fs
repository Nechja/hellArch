namespace HellArch.DataTypes.Auth

open System

/// OAuth provider account linked to a user.
Stores provider-specific identity info. Token storage is optional
(only needed if we want to call provider APIs on behalf of user).
type LinkedAccount = {
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
}
