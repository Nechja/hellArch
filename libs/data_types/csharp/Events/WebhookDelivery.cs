namespace HellArch.DataTypes.Events;

/// <summary>
/// Record of a webhook delivery attempt.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public required string EventId { get; set; }
    public required string EventType { get; set; }
    public string? Payload { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public bool Success { get; set; }
    public int Attempt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties (EF Core)
    public Webhook? Webhook { get; set; }
}
