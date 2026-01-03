namespace HellArch.DataTypes.Events

open System

type WebhookFormat =
    | Raw
    | Discord
    | Slack

/// Webhook endpoint registration for event delivery.
type Webhook = {
    Id: Guid
    Name: string
    Url: string
    Secret: string
    EventTypes: string list
    Format: WebhookFormat
    IsActive: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    Deliveries: WebhookDelivery list
}
