namespace HellArch.DataTypes.Events;

/// <summary>
/// Webhook payload format for delivery transformation.
/// </summary>
public enum WebhookFormat
{
    /// <summary>Raw CloudEvent JSON payload.</summary>
    Raw,
    /// <summary>Discord webhook format with embeds.</summary>
    Discord,
    /// <summary>Slack webhook format with blocks.</summary>
    Slack
}

/// <summary>
/// Webhook endpoint registration for event delivery.
/// </summary>
public class Webhook
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required string Secret { get; set; }
    public List<string> EventTypes { get; set; } = [];
    public WebhookFormat Format { get; set; } = WebhookFormat.Raw;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    public ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}
