namespace HellArch.DataTypes.Events;

/// <summary>
/// A registered webhook endpoint that receives event notifications.
/// </summary>
public class Webhook
{
    public Guid Id { get; set; }
    public required string Url { get; set; }
    public required string Secret { get; set; }
    public required List<string> EventTypes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WebhookDelivery> Deliveries { get; set; } = [];
}
