namespace HellArch.DataTypes.Events

open System

/// A registered webhook endpoint that receives event notifications.
type Webhook = {
    Id: Guid
    Url: string
    Secret: string
    EventTypes: string list
    IsActive: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    Deliveries: WebhookDelivery list
}
