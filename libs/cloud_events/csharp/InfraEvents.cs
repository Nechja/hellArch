namespace HellArch.CloudEvents.Infra;

/// <summary>
/// Published when a service becomes unhealthy.
/// Event type: hellarch.infra.service.unhealthy.v1
/// </summary>
public class ServiceUnhealthy
{
    public required string ServiceName { get; init; }
    public required string Status { get; init; }
    public required int ConsecutiveFailures { get; init; }
    public string? LastError { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }
}

/// <summary>
/// Published when a service recovers from unhealthy state.
/// Event type: hellarch.infra.service.recovered.v1
/// </summary>
public class ServiceRecovered
{
    public required string ServiceName { get; init; }
    public required DateTimeOffset RecoveredAt { get; init; }
    public required int DowntimeSeconds { get; init; }
}
