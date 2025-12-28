namespace HellArch.CloudEvents.Auth

open System

/// Published when user info is updated.
/// Event type: hellarch.auth.user.updated.v1
type UserUpdated = {
    UserId: string
    Email: string
    Username: string
    UpdatedAt: DateTimeOffset
}
