namespace HellArch.CloudEvents.Auth

open System

/// Published when an OAuth account is unlinked from a user.
/// Event type: hellarch.auth.account.unlinked.v1
[<CLIMutable>]
type AccountUnlinked = {
    user_id: string
    provider: string
    unlinked_at: DateTimeOffset
}
