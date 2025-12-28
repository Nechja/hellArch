namespace HellArch.CloudEvents.Auth

open System

/// Published when a new user is created via OAuth.
/// Event type: hellarch.auth.user.created.v1
type UserCreated = {
    UserId: string
    Email: string
    Username: string
    Provider: string
    ProviderUserId: string
    AvatarUrl: string
    CreatedAt: DateTimeOffset
}
