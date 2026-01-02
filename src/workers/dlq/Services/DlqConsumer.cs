using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HellArch.CloudEvents;
using HellArch.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DlqWorker.Services;

/// <summary>
/// Background service that consumes failed events from the Dead Letter Queue.
/// Logs failed event details for debugging and monitoring.
/// </summary>
public sealed class DlqConsumer(
    IConfiguration configuration,
    ILogger<DlqConsumer> logger) : RabbitMqConsumerBase(configuration, logger)
{
    private static readonly ActivitySource ActivitySource = new("dlq-worker");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectToDlq(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(Channel!);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var activity = StartActivity(ea);

            try
            {
                await ProcessDeadLetter(ea, stoppingToken);
                await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
#pragma warning disable CA1031 // DLQ handler must catch all exceptions to prevent message loss
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Logger.LogError(ex, "Failed to process dead letter event");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                // Don't requeue - this is already a dead letter
                await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await Channel!.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        Logger.LogInformation("Started consuming dead letters from queue: {Queue}", QueueName);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectToDlq(CancellationToken ct)
    {
        QueueName = Configuration["RabbitMQ:QueueName"] ?? "hellarch.events.dlq";
        var dlqExchange = "hellarch.events.dlq";

        (Connection, Channel) = await CreateConnection(ct);

        // Declare the DLQ exchange (should already exist from realtime worker)
        await Channel.ExchangeDeclareAsync(
            exchange: dlqExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        // Declare and bind the DLQ
        await Channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await Channel.QueueBindAsync(
            queue: QueueName,
            exchange: dlqExchange,
            routingKey: "",
            cancellationToken: ct);

        Logger.LogInformation("Connected to RabbitMQ DLQ, queue: {Queue}", QueueName);
    }

    private Task ProcessDeadLetter(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var headers = ea.BasicProperties.Headers;

        // Extract original routing info from headers
        var originalExchange = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathExchange);
        var originalQueue = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathQueue);
        var deathReason = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathReason);
        var eventType = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventType);
        var eventId = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventId);
        var eventSource = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventSource);

        // Try to parse the CloudEvent
        CloudEventDto? cloudEvent = null;
        try
        {
            cloudEvent = JsonSerializer.Deserialize<CloudEventDto>(body, CloudEventJson.Options);
        }
        catch (JsonException)
        {
            // Body is not valid JSON - log the raw body
        }

        Logger.LogError(
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
        var eventType = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, RabbitMqHeaders.CloudEventType);

        var activity = ActivitySource.StartActivity(
            "dlq.process_dead_letter",
            ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.cloudevents.type", eventType);
        activity?.SetTag("dlq.is_dead_letter", true);

        return activity;
    }
}
