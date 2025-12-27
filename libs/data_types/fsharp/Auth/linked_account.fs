namespace HellArch.DataTypes.Auth

open System

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
    ExpiresAt: DateTime option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
