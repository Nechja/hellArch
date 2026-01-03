namespace HellArch.DataTypes.Events

open System

/// Dead letter entry for tracking failed events that exhausted retries.
type DeadLetter = {
    Id: Guid
    EventType: string
    EventId: string
    Source: string
    OriginalQueue: string
    DeathReason: string
    RetryCount: int
    Body: string
    ErrorMessage: string option
    CreatedAt: DateTime
}
