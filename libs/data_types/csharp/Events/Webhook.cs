namespace HellArch.DataTypes.Events;

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
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    public ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}
