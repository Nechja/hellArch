namespace HellArch.CloudEvents.Infra

open System

/// Published when a service becomes unhealthy.
/// Event type: hellarch.infra.service.unhealthy.v1
[<CLIMutable>]
type ServiceUnhealthy = {
    service_name: string
    status: string
    consecutive_failures: int
    last_error: string option
    detected_at: DateTimeOffset
}
