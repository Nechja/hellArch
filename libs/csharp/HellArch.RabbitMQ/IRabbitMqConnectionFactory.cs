using RabbitMQ.Client;

namespace HellArch.RabbitMQ;

/// <summary>
/// Factory abstraction for creating RabbitMQ connections and channels.
/// Enables dependency injection and testability for RabbitMQ consumers.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    /// <summary>
    /// Creates a new connection and channel to RabbitMQ.
    /// </summary>
    Task<(IConnection Connection, IChannel Channel)> Create(CancellationToken ct = default);

    /// <summary>
    /// Creates a new connection and channel with retry logic for initial failures.
    /// </summary>
    Task<(IConnection Connection, IChannel Channel)> CreateWithRetry(
        int maxAttempts = 0,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        CancellationToken ct = default);
}
