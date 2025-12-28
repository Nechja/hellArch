namespace HellArch.CloudEvents.Auth

open System

type SessionEndReason =
    | Logout
    | Expired
    | Revoked

/// Published when a session ends (logout or expiry).
/// Event type: hellarch.auth.session.ended.v1
type SessionEnded = {
    SessionId: string
    UserId: string
    Reason: SessionEndReason
    EndedAt: DateTimeOffset
}
