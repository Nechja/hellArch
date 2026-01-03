namespace HellArch.DataTypes.Events;

/// <summary>
/// Dead letter entry for tracking failed events that exhausted retries.
/// </summary>
public class DeadLetter
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string EventId { get; set; }
    public required string Source { get; set; }
    public required string OriginalQueue { get; set; }
    public required string DeathReason { get; set; }
    public int RetryCount { get; set; }
    public required string Body { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
