namespace HellArch.CloudEvents.Auth

open System

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

[<CLIMutable>]
type UserUpdated = {
    user_id: string
    email: string
    username: string
    avatar_url: string
    updated_at: DateTimeOffset
}

[<CLIMutable>]
type UserDeleted = {
    user_id: string
    deleted_at: DateTimeOffset
}

[<CLIMutable>]
type AccountLinked = {
    user_id: string
    provider: string
    provider_user_id: string
    email: string
    linked_at: DateTimeOffset
}

[<CLIMutable>]
type AccountUnlinked = {
    user_id: string
    provider: string
    provider_user_id: string
    unlinked_at: DateTimeOffset
}

[<CLIMutable>]
type SessionCreated = {
    session_id: string
    user_id: string
    ip_address: string
    user_agent: string
    created_at: DateTimeOffset
    expires_at: DateTimeOffset
}

type SessionEndReason =
    | Logout
    | Expired
    | Revoked

[<CLIMutable>]
type SessionEnded = {
    session_id: string
    user_id: string
    reason: SessionEndReason
    ended_at: DateTimeOffset
}
