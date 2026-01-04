namespace HellArch.DataTypes.Identity

open System

/// OAuth 2.0 access token (short-lived, 1 hour).
/// Stored for validation until expiry.
type AccessToken = {
    Token: string
    UserId: Guid
    ClientId: string
    Scope: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
    User: IdentityUser option
}
