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
