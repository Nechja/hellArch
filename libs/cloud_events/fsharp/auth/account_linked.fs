namespace HellArch.CloudEvents.Auth

open System

/// Published when an OAuth account is linked to a user.
/// Event type: hellarch.auth.account.linked.v1
[<CLIMutable>]
type AccountLinked = {
    user_id: string
    provider: string
    provider_user_id: string
    email: string
    linked_at: DateTimeOffset
}
