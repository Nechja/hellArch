namespace HellArch.DataTypes.Events;

/// <summary>
/// Audit log entry for tracking system events.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string EventId { get; set; }
    public required string Source { get; set; }
    public string? ActorId { get; set; }
    public string? TargetId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
