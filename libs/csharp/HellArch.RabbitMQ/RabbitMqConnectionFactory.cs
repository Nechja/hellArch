using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HellArch.RabbitMQ;

/// <summary>
/// Default implementation of IRabbitMqConnectionFactory that creates connections
/// with automatic recovery enabled.
/// </summary>
public sealed class RabbitMqConnectionFactory(
    IConfiguration configuration,
    ILogger<RabbitMqConnectionFactory> logger) : IRabbitMqConnectionFactory
{
    public async Task<(IConnection Connection, IChannel Channel)> Create(CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            Uri = GetRabbitMqUri(),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true
        };

        var connection = await factory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        return (connection, channel);
    }

    public async Task<(IConnection Connection, IChannel Channel)> CreateWithRetry(
        int maxAttempts = 0,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        CancellationToken ct = default)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        var maxDelayValue = maxDelay ?? TimeSpan.FromSeconds(30);
        var attempt = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                attempt++;
                return await Create(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (maxAttempts > 0 && attempt >= maxAttempts)
                {
                    logger.LogError(ex, "Max connection attempts ({MaxAttempts}) reached", maxAttempts);
                    throw;
                }

                logger.LogWarning(ex, "Failed to connect to RabbitMQ, retrying in {Delay}... (attempt {Attempt})",
                    delay, attempt);

                await Task.Delay(delay, ct);

                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, maxDelayValue.TotalMilliseconds));
            }
        }

        throw new OperationCanceledException(ct);
    }

    private Uri GetRabbitMqUri()
    {
        var rabbitUrl = configuration["RabbitMQ:Url"];
        if (string.IsNullOrEmpty(rabbitUrl))
        {
            rabbitUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");
        }

        if (string.IsNullOrEmpty(rabbitUrl))
        {
            throw new InvalidOperationException("RabbitMQ:Url configuration or RABBITMQ_URL environment variable is required");
        }

        return new Uri(rabbitUrl);
    }
}
