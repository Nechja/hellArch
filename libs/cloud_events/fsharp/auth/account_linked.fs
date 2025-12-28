namespace HellArch.CloudEvents.Auth

open System

/// Published when an OAuth account is linked to a user.
/// Event type: hellarch.auth.account.linked.v1
type AccountLinked = {
    UserId: string
    Provider: string
    ProviderUserId: string
    Email: string
    LinkedAt: DateTimeOffset
}
