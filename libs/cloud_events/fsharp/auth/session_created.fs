namespace HellArch.CloudEvents.Auth

open System

/// Published when a new session is created.
/// Event type: hellarch.auth.session.created.v1
[<CLIMutable>]
type SessionCreated = {
    session_id: string
    user_id: string
    ip_address: string
    user_agent: string
    created_at: DateTimeOffset
    expires_at: DateTimeOffset
}
