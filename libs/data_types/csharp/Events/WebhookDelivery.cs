namespace HellArch.DataTypes.Events;

/// <summary>
/// Tracks each attempt to deliver an event to a webhook endpoint.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public required string EventId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? Error { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public Webhook Webhook { get; set; } = null!;
}
