using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HellArch.RabbitMQ;

/// <summary>
/// Health check that verifies RabbitMQ connection status.
/// </summary>
public sealed class RabbitMqHealthCheck(Func<bool> connectionCheck) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isConnected = connectionCheck();

        return Task.FromResult(isConnected
            ? HealthCheckResult.Healthy("RabbitMQ connection is active")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is not active"));
    }
}
