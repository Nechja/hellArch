namespace HellArch.DataTypes.Identity

open System

/// OAuth 2.0 refresh token (long-lived, 30 days).
type RefreshToken = {
    Token: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
    User: IdentityUser option
}
