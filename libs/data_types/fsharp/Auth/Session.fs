namespace HellArch.DataTypes.Auth

open System

/// User session for cookie-based authentication.
type Session = {
    Id: Guid
    UserId: Guid
    Token: string
    IpAddress: string option
    UserAgent: string option
    ExpiresAt: DateTime
    CreatedAt: DateTime
}
