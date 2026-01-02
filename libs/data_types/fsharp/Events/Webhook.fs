namespace HellArch.DataTypes.Events

open System

/// Webhook endpoint registration for event delivery.
type Webhook = {
    Id: Guid
    Name: string
    Url: string
    Secret: string
    EventTypes: string list
    IsActive: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    Deliveries: WebhookDelivery list
}
