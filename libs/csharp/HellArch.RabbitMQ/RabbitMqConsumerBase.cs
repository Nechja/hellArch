using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HellArch.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ consumers that handles connection management,
/// automatic reconnection, and lifecycle.
/// </summary>
public abstract class RabbitMqConsumerBase(
    IConfiguration configuration,
    ILogger logger) : BackgroundService
{
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    protected IConfiguration Configuration { get; } = configuration;
    protected ILogger Logger { get; } = logger;
    protected IConnection? Connection { get; set; }
    protected IChannel? Channel { get; set; }
    protected string QueueName { get; set; } = null!;

    /// <summary>
    /// Gets whether the RabbitMQ connection and channel are open.
    /// </summary>
    public bool IsConnected => Connection?.IsOpen == true && Channel?.IsOpen == true;

    /// <summary>
    /// Gets the RabbitMQ URL from configuration or environment variables.
    /// </summary>
    protected Uri GetRabbitMqUri()
    {
        var rabbitUrl = Configuration["RabbitMQ:Url"];
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

    /// <summary>
    /// Creates a connection and channel to RabbitMQ with automatic recovery enabled.
    /// </summary>
    protected async Task<(IConnection connection, IChannel channel)> CreateConnection(CancellationToken ct)
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

    /// <summary>
    /// Creates a connection with retry logic for initial connection failures.
    /// </summary>
    protected async Task<(IConnection connection, IChannel channel)> CreateConnectionWithRetry(
        CancellationToken ct,
        int maxAttempts = 0,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        var maxDelayValue = maxDelay ?? TimeSpan.FromSeconds(30);
        var attempt = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                attempt++;
                return await CreateConnection(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (maxAttempts > 0 && attempt >= maxAttempts)
                {
                    Logger.LogError(ex, "Max connection attempts ({MaxAttempts}) reached", maxAttempts);
                    throw;
                }

                Logger.LogWarning(ex, "Failed to connect to RabbitMQ, retrying in {Delay}... (attempt {Attempt})",
                    delay, attempt);

                await Task.Delay(delay, ct);

                // Exponential backoff with cap
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, maxDelayValue.TotalMilliseconds));
            }
        }

        throw new OperationCanceledException(ct);
    }

    /// <summary>
    /// Attempts to reconnect to RabbitMQ with exponential backoff.
    /// </summary>
    protected async Task<bool> TryReconnect(CancellationToken ct)
    {
        if (!await _reconnectLock.WaitAsync(0, ct))
        {
            // Another reconnection is in progress
            return false;
        }

        try
        {
            Logger.LogWarning("Attempting to reconnect to RabbitMQ...");

            // Close existing connections
            await CloseConnectionsAsync();

            // Attempt reconnection
            (Connection, Channel) = await CreateConnectionWithRetry(ct);

            Logger.LogInformation("Successfully reconnected to RabbitMQ");
            return true;
        }
#pragma warning disable CA1031 // Reconnection must catch all exceptions to handle any failure gracefully
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError(ex, "Failed to reconnect to RabbitMQ");
            return false;
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private async Task CloseConnectionsAsync()
    {
        try
        {
            if (Channel is not null)
            {
                await Channel.CloseAsync();
                Channel.Dispose();
                Channel = null;
            }
        }
#pragma warning disable CA1031 // Cleanup must ignore all exceptions
        catch { /* Ignore errors during cleanup */ }
#pragma warning restore CA1031

        try
        {
            if (Connection is not null)
            {
                await Connection.CloseAsync();
                Connection.Dispose();
                Connection = null;
            }
        }
#pragma warning disable CA1031 // Cleanup must ignore all exceptions
        catch { /* Ignore errors during cleanup */ }
#pragma warning restore CA1031
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CloseConnectionsAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
        _reconnectLock.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
