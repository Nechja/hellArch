namespace HellArch.DataTypes.Events

open System

/// Tracks each attempt to deliver an event to a webhook endpoint.
type WebhookDelivery = {
    Id: Guid
    WebhookId: Guid
    EventId: string
    EventType: string
    Payload: string
    StatusCode: int option
    ResponseBody: string option
    Error: string option
    AttemptNumber: int
    CreatedAt: DateTime
    Webhook: Webhook
}
