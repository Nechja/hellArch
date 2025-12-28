namespace HellArch.CloudEvents.Auth

open System

/// Published when a user is deleted.
/// Event type: hellarch.auth.user.deleted.v1
type UserDeleted = {
    UserId: string
    DeletedAt: DateTimeOffset
}
