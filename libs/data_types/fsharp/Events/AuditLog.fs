namespace HellArch.DataTypes.Events

open System

/// Stores a record of every event that passes through the system for audit purposes.
type AuditLog = {
    Id: Guid
    EventType: string
    EventId: string
    Source: string
    ActorId: Guid option
    TargetId: Guid option
    Metadata: string option
    CreatedAt: DateTime
}
