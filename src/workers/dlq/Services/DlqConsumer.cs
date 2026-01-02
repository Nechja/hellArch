using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DlqWorker.Services;

/// <summary>
/// Background service that consumes failed events from the Dead Letter Queue.
/// Logs failed event details for debugging and monitoring.
/// </summary>
public sealed class DlqConsumer(
    IConfiguration configuration,
    ILogger<DlqConsumer> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("dlq-worker");

    private IConnection? _connection;
    private IChannel? _channel;
    private string _queueName = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Connect(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var activity = StartActivity(ea);

            try
            {
                await ProcessDeadLetter(ea, stoppingToken);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process dead letter event");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                // Don't requeue - this is already a dead letter
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Started consuming dead letters from queue: {Queue}", _queueName);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task Connect(CancellationToken ct)
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

        _queueName = configuration["RabbitMQ:QueueName"] ?? "realtime-worker.dlq";
        var dlqExchange = "hellarch.events.dlq";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUrl)
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        // Declare the DLQ exchange (should already exist from realtime worker)
        await _channel.ExchangeDeclareAsync(
            exchange: dlqExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // Declare and bind the DLQ
        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: dlqExchange,
            routingKey: "",
            cancellationToken: ct);

        logger.LogInformation("Connected to RabbitMQ DLQ, queue: {Queue}", _queueName);
    }

    private Task ProcessDeadLetter(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);

        // Extract original routing info from headers
        var originalExchange = GetHeader(ea.BasicProperties.Headers, "x-first-death-exchange");
        var originalQueue = GetHeader(ea.BasicProperties.Headers, "x-first-death-queue");
        var deathReason = GetHeader(ea.BasicProperties.Headers, "x-first-death-reason");
        var eventType = GetHeader(ea.BasicProperties.Headers, "ce-type");
        var eventId = GetHeader(ea.BasicProperties.Headers, "ce-id");
        var eventSource = GetHeader(ea.BasicProperties.Headers, "ce-source");

        // Try to parse the CloudEvent
        CloudEventInfo? cloudEvent = null;
        try
        {
            cloudEvent = JsonSerializer.Deserialize<CloudEventInfo>(body, JsonOptions.Default);
        }
        catch (JsonException)
        {
            // Body is not valid JSON - log the raw body
        }

        logger.LogError(
            "Dead letter received: Type={EventType}, Id={EventId}, Source={EventSource}, " +
            "OriginalQueue={OriginalQueue}, DeathReason={DeathReason}, Body={Body}",
            eventType ?? cloudEvent?.Type ?? "unknown",
            eventId ?? cloudEvent?.Id ?? "unknown",
            eventSource ?? cloudEvent?.Source ?? "unknown",
            originalQueue ?? "unknown",
            deathReason ?? "unknown",
            body.Length > 1000 ? body[..1000] + "..." : body);

        // TODO: Future enhancements:
        // - Store failed events in a database for manual retry
        // - Send alerts for specific event types
        // - Implement automatic retry with exponential backoff
        // - Publish metrics for monitoring

        return Task.CompletedTask;
    }

    private static Activity? StartActivity(BasicDeliverEventArgs ea)
    {
        var eventType = GetHeader(ea.BasicProperties.Headers, "ce-type");

        var activity = ActivitySource.StartActivity(
            "dlq.process_dead_letter",
            ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.cloudevents.type", eventType);
        activity?.SetTag("dlq.is_dead_letter", true);

        return activity;
    }

    private static string? GetHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        if (!headers.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string s => s,
            null => null,
            _ => value.ToString()
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Minimal CloudEvent structure for logging purposes.
/// </summary>
internal sealed record CloudEventInfo
{
    public string? Id { get; init; }
    public string? Source { get; init; }
    public string? Type { get; init; }
    public DateTimeOffset? Time { get; init; }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
