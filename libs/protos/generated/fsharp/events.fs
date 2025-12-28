namespace HellArch.Events.V1

open System

type SessionEndReason =
    | Logout
    | Expired
    | Revoked

type UserCreated = {
    UserId: string
    Email: string
    Username: string
    Provider: string
    ProviderUserId: string
    AvatarUrl: string
    CreatedAt: DateTime option
}

and UserUpdated = {
    UserId: string
    Email: string
    Username: string
    UpdatedAt: DateTime option
}

and UserDeleted = {
    UserId: string
    DeletedAt: DateTime option
}

and SessionCreated = {
    SessionId: string
    UserId: string
    IpAddress: string
    UserAgent: string
    CreatedAt: DateTime option
    ExpiresAt: DateTime option
}

and SessionEnded = {
    SessionId: string
    UserId: string
    Reason: SessionEndReason
    EndedAt: DateTime option
}

and AccountLinked = {
    UserId: string
    Provider: string
    ProviderUserId: string
    Email: string
    LinkedAt: DateTime option
}

and AccountUnlinked = {
    UserId: string
    Provider: string
    UnlinkedAt: DateTime option
}

