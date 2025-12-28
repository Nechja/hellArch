namespace HellArch.CloudEvents.Auth

open System

/// Published when a new user is created via OAuth.
/// Event type: hellarch.auth.user.created.v1
[<CLIMutable>]
type UserCreated = {
    user_id: string
    email: string
    username: string
    provider: string
    provider_user_id: string
    avatar_url: string
    created_at: DateTimeOffset
}
