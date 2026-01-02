namespace HellArch.DataTypes.Events;

/// <summary>
/// Stores a record of every event that passes through the system for audit purposes.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string EventId { get; set; }
    public required string Source { get; set; }
    public Guid? ActorId { get; set; }
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
