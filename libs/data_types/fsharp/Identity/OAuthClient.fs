namespace HellArch.DataTypes.Identity

open System

/// OAuth 2.0 client application registration.
type OAuthClient = {
    Id: string
    SecretHash: string
    Name: string
    RedirectUris: string list
    IsActive: bool
    CreatedAt: DateTime
}
