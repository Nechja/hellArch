namespace HellArch.CloudEvents.Auth

open System

/// Published when user info is updated.
/// Event type: hellarch.auth.user.updated.v1
[<CLIMutable>]
type UserUpdated = {
    user_id: string
    email: string
    username: string
    updated_at: DateTimeOffset
}
