namespace HellArch.CloudEvents.Auth

open System

/// Published when a user is deleted.
/// Event type: hellarch.auth.user.deleted.v1
[<CLIMutable>]
type UserDeleted = {
    user_id: string
    deleted_at: DateTimeOffset
}
