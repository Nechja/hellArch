using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HellArch.RabbitMQ;

/// <summary>
/// Base class for RabbitMQ consumers that handles connection management and lifecycle.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    protected readonly IConfiguration Configuration;
    protected readonly ILogger Logger;

    protected IConnection? Connection;
    protected IChannel? Channel;
    protected string QueueName = null!;

    protected RabbitMqConsumerBase(IConfiguration configuration, ILogger logger)
    {
        Configuration = configuration;
        Logger = logger;
    }

    /// <summary>
    /// Gets the RabbitMQ URL from configuration or environment variables.
    /// </summary>
    protected string GetRabbitMqUrl()
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

        return rabbitUrl;
    }

    /// <summary>
    /// Creates a connection and channel to RabbitMQ.
    /// </summary>
    protected async Task<(IConnection connection, IChannel channel)> CreateConnection(CancellationToken ct)
    {
        var rabbitUrl = GetRabbitMqUrl();

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUrl)
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
