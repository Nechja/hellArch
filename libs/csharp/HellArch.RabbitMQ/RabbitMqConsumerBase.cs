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
    IRabbitMqConnectionFactory connectionFactory,
    IConfiguration configuration,
    ILogger logger) : BackgroundService
{
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    /// <summary>
    /// Lock for synchronizing access to Connection and Channel.
    /// Derived classes should use this when atomicity is required.
    /// </summary>
    protected Lock ConnectionLock { get; } = new();

    protected IConnection? Connection { get; set; }
    protected IChannel? Channel { get; set; }

    protected IRabbitMqConnectionFactory ConnectionFactory { get; } = connectionFactory;
    protected IConfiguration Configuration { get; } = configuration;
    protected ILogger Logger { get; } = logger;
    protected string QueueName { get; set; } = null!;

    /// <summary>
    /// Gets whether the RabbitMQ connection and channel are open.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            using (ConnectionLock.EnterScope())
            {
                return Connection?.IsOpen == true && Channel?.IsOpen == true;
            }
        }
    }

    /// <summary>
    /// Creates a connection and channel to RabbitMQ with automatic recovery enabled.
    /// </summary>
    protected Task<(IConnection Connection, IChannel Channel)> CreateConnection(CancellationToken ct) =>
        ConnectionFactory.Create(ct);

    /// <summary>
    /// Creates a connection with retry logic for initial connection failures.
    /// </summary>
    protected Task<(IConnection Connection, IChannel Channel)> CreateConnectionWithRetry(
        CancellationToken ct,
        int maxAttempts = 0,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null) =>
        ConnectionFactory.CreateWithRetry(maxAttempts, initialDelay, maxDelay, ct);

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
        IChannel? channelToClose;
        IConnection? connectionToClose;

        using (ConnectionLock.EnterScope())
        {
            channelToClose = Channel;
            connectionToClose = Connection;
            Channel = null;
            Connection = null;
        }

        try
        {
            if (channelToClose is not null)
            {
                await channelToClose.CloseAsync();
                channelToClose.Dispose();
            }
        }
#pragma warning disable CA1031 // Cleanup must ignore all exceptions
        catch { /* Ignore errors during cleanup */ }
#pragma warning restore CA1031

        try
        {
            if (connectionToClose is not null)
            {
                await connectionToClose.CloseAsync();
                connectionToClose.Dispose();
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
        using (ConnectionLock.EnterScope())
        {
            Channel?.Dispose();
            Connection?.Dispose();
        }
        _reconnectLock.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
