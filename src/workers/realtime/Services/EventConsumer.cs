using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Realtime.Hubs;

namespace Realtime.Services;

/// <summary>
/// Background service that consumes CloudEvents from RabbitMQ
/// and broadcasts them to connected SignalR clients.
/// </summary>
public sealed class EventConsumer(
    IHubContext<EventsHub, IEventsClient> hubContext,
    IConfiguration configuration,
    ILogger<EventConsumer> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("realtime-worker");

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
                await ProcessEvent(ea, stoppingToken);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize event");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to process event");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Started consuming events from queue: {Queue}", _queueName);

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

        var exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "hellarch.events";
        _queueName = configuration["RabbitMQ:QueueName"] ?? "realtime-worker";
        var routingKey = configuration["RabbitMQ:RoutingKey"] ?? "hellarch.#";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitUrl)
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // Dead Letter Queue exchange - failed messages route here
        var dlqExchange = "hellarch.events.dlq";
        await _channel.ExchangeDeclareAsync(
            exchange: dlqExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // DLQ queue for this worker
        var dlqQueue = $"{_queueName}.dlq";
        await _channel.QueueDeclareAsync(
            queue: dlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: dlqQueue,
            exchange: dlqExchange,
            routingKey: "",
            cancellationToken: ct);

        // Main queue with DLQ routing
        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlqExchange
        };

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: ct);

        logger.LogInformation(
            "Connected to RabbitMQ, queue: {Queue}, DLQ: {DlqQueue}, routing key: {RoutingKey}",
            _queueName,
            dlqQueue,
            routingKey);
    }

    private async Task ProcessEvent(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var cloudEvent = JsonSerializer.Deserialize<CloudEventDto>(body, JsonOptions.Default);

        if (cloudEvent is null)
        {
            logger.LogWarning("Failed to deserialize CloudEvent");
            return;
        }

        logger.LogDebug(
            "Processing event: {EventType}, Id: {EventId}",
            cloudEvent.Type,
            cloudEvent.Id);

        await BroadcastEvent(cloudEvent, ct);
    }

    private async Task BroadcastEvent(CloudEventDto cloudEvent, CancellationToken ct)
    {
        var eventType = cloudEvent.Type;
        var userId = cloudEvent.GetUserId();

        // Send to event-type specific subscribers
        await hubContext.Clients
            .Group($"event:{eventType}")
            .ReceiveEvent(cloudEvent);

        // Send to user-specific group if userId is present
        if (userId is not null)
        {
            await hubContext.Clients
                .Group($"{EventsHub.UserGroupPrefix}{userId}")
                .ReceiveEvent(cloudEvent);

            // Handle specific event types with dedicated methods
            if (eventType.Contains("session.ended", StringComparison.OrdinalIgnoreCase))
            {
                var reason = GetSessionEndReason(cloudEvent);
                await hubContext.Clients
                    .Group($"{EventsHub.UserGroupPrefix}{userId}")
                    .SessionInvalidated(reason);
            }

            if (eventType.Contains("user.updated", StringComparison.OrdinalIgnoreCase))
            {
                await hubContext.Clients
                    .Group($"{EventsHub.UserGroupPrefix}{userId}")
                    .UserUpdated();
            }
        }

        // Admin events go to admin group
        if (IsAdminEvent(eventType))
        {
            await hubContext.Clients
                .Group(EventsHub.AdminGroup)
                .ReceiveEvent(cloudEvent);
        }

        logger.LogDebug(
            "Broadcasted event: {EventType} to groups",
            eventType);
    }

    private static string GetSessionEndReason(CloudEventDto cloudEvent)
    {
        if (cloudEvent.Data is not { } data)
        {
            return "unknown";
        }

        if (data.TryGetProperty("reason", out var reason))
        {
            return reason.GetString() ?? "unknown";
        }

        return "unknown";
    }

    private static bool IsAdminEvent(string eventType) =>
        eventType.Contains("user.created", StringComparison.OrdinalIgnoreCase)
        || eventType.Contains("user.deleted", StringComparison.OrdinalIgnoreCase);

    private static Activity? StartActivity(BasicDeliverEventArgs ea)
    {
        var eventType = GetHeader(ea.BasicProperties.Headers, "ce-type");

        var activity = ActivitySource.StartActivity(
            "realtime.process_event",
            ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.cloudevents.type", eventType);

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

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
