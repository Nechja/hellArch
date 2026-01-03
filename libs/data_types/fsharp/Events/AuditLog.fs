namespace HellArch.DataTypes.Events

open System

/// Audit log entry for tracking system events.
type AuditLog = {
    Id: Guid
    EventType: string
    EventId: string
    Source: string
    ActorId: string option
    TargetId: string option
    Metadata: string option
    CreatedAt: DateTime
}
