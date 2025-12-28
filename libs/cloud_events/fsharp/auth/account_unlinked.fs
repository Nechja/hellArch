namespace HellArch.CloudEvents.Auth

open System

/// Published when an OAuth account is unlinked from a user.
/// Event type: hellarch.auth.account.unlinked.v1
type AccountUnlinked = {
    UserId: string
    Provider: string
    UnlinkedAt: DateTimeOffset
}
