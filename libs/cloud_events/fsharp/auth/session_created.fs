namespace HellArch.CloudEvents.Auth

open System

type SessionEndReason =
    | Logout
    | Expired
    | Revoked

/// Published when a new session is created.
/// Event type: hellarch.auth.session.created.v1
type SessionCreated = {
    SessionId: string
    UserId: string
    IpAddress: string
    UserAgent: string
    CreatedAt: DateTimeOffset
    ExpiresAt: DateTimeOffset
}
