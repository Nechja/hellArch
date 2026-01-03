namespace HellArch.CloudEvents.Infra

open System

/// Published when a service recovers from unhealthy state.
/// Event type: hellarch.infra.service.recovered.v1
[<CLIMutable>]
type ServiceRecovered = {
    service_name: string
    recovered_at: DateTimeOffset
    downtime_seconds: int
}
