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

/// Record of a webhook delivery attempt.
and WebhookDelivery = {
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

/// Dead letter entry for tracking failed events that exhausted retries.
and DeadLetter = {
    Id: Guid
    EventType: string
    EventId: string
    Source: string
    OriginalQueue: string
    DeathReason: string
    RetryCount: int
    Body: string
    ErrorMessage: string option
    CreatedAt: DateTime
}

/// Audit log entry for tracking system events.
and AuditLog = {
    Id: Guid
    EventType: string
    EventId: string
    Source: string
    ActorId: string option
    TargetId: string option
    Metadata: string option
    CreatedAt: DateTime
}

