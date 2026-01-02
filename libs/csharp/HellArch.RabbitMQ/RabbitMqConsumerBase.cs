using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HellArch.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ consumers that handles connection management and lifecycle.
/// </summary>
public abstract class RabbitMqConsumerBase(
    IConfiguration configuration,
    ILogger logger) : BackgroundService
{
    protected IConfiguration Configuration { get; } = configuration;
    protected ILogger Logger { get; } = logger;
    protected IConnection? Connection { get; set; }
    protected IChannel? Channel { get; set; }
    protected string QueueName { get; set; } = null!;

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
    /// Creates a connection and channel to RabbitMQ.
    /// </summary>
    protected async Task<(IConnection connection, IChannel channel)> CreateConnection(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = GetRabbitMqUri()
        };

        var connection = await factory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        return (connection, channel);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Channel is not null)
        {
            await Channel.CloseAsync(cancellationToken);
        }

        if (Connection is not null)
        {
            await Connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
