namespace HellArch.CloudEvents.Auth

open System

type SessionEndReason =
    | Logout
    | Expired
    | Revoked

/// Published when a session ends (logout or expiry).
/// Event type: hellarch.auth.session.ended.v1
[<CLIMutable>]
type SessionEnded = {
    session_id: string
    user_id: string
    reason: SessionEndReason
    ended_at: DateTimeOffset
}
