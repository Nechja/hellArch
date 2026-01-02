namespace HellArch.DataTypes.Events

open System

/// Record of a webhook delivery attempt.
type WebhookDelivery = {
    Id: Guid
    WebhookId: Guid
    EventId: string
    EventType: string
    Payload: string option
    StatusCode: int
    ResponseBody: string option
    Success: bool
    Attempt: int
    CreatedAt: DateTime
    Webhook: Webhook option
}
