using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using HellArch.CloudEvents;
using HellArch.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DlqWorker.Services;

/// <summary>
/// Background service that consumes failed events from the Dead Letter Queue.
/// Implements retry with backoff, storage for permanently failed events, and metrics.
/// </summary>
public sealed class DlqConsumer(
    IRabbitMqConnectionFactory connectionFactory,
    IConfiguration configuration,
    EventsApiClient eventsApiClient,
    ILogger<DlqConsumer> logger) : RabbitMqConsumerBase(connectionFactory, configuration, logger)
{
    private static readonly ActivitySource ActivitySource = new("dlq-worker");
    private static readonly Meter Meter = new("dlq-worker");

    private static readonly Counter<long> DeadLettersReceived = Meter.CreateCounter<long>(
        "dlq.messages.received",
        description: "Total dead letters received");

    private static readonly Counter<long> RetriesAttempted = Meter.CreateCounter<long>(
        "dlq.retries.attempted",
        description: "Total retry attempts");

    private static readonly Counter<long> PermanentlyFailed = Meter.CreateCounter<long>(
        "dlq.messages.permanently_failed",
        description: "Messages that exhausted all retries");

    private static readonly Counter<long> StorageSuccesses = Meter.CreateCounter<long>(
        "dlq.storage.successes",
        description: "Successfully stored dead letters");

    private static readonly Counter<long> StorageFailures = Meter.CreateCounter<long>(
        "dlq.storage.failures",
        description: "Failed storage attempts");

    private readonly int _maxRetries = configuration.GetValue("DLQ:MaxRetries", 3);
    private readonly int _baseDelaySeconds = configuration.GetValue("DLQ:BaseDelaySeconds", 5);
    private readonly int _maxBodySizeBytes = configuration.GetValue("DLQ:MaxBodySizeBytes", 65536); // 64KB default
    private readonly SemaphoreSlim _publishChannelLock = new(1, 1);

    // Fields that should be redacted from stored bodies to prevent PII leakage
    // Uses partial matching - any field containing these strings will be redacted
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "apikey", "api_key", "authorization",
        "credential", "ssn", "social_security", "credit_card", "card_number",
        "access_token", "refresh_token", "session_token", "private_key",
        "cvv", "pin", "security_code", "routing_number", "account_number",
        "bank_account", "tax_id", "passport", "license_number", "dob",
        "date_of_birth", "mother_maiden", "security_question", "security_answer"
    };

    private IChannel? _publishChannel;
    private volatile bool _stopping;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectToDlq(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(Channel!);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var activity = StartActivity(ea);
            var eventType = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, RabbitMqHeaders.CloudEventType) ?? "unknown";

            DeadLettersReceived.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

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
        var mainExchange = Configuration["RabbitMQ:MainExchange"] ?? "hellarch.events";

        (Connection, Channel) = await CreateConnection(ct);
        _publishChannel = await Connection!.CreateChannelAsync(cancellationToken: ct);

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

        // Ensure main exchange exists for retry publishing
        await _publishChannel.ExchangeDeclareAsync(
            exchange: mainExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        Logger.LogInformation(
            "Connected to RabbitMQ DLQ, queue: {Queue}, max retries: {MaxRetries}",
            QueueName, _maxRetries);
    }

    private async Task ProcessDeadLetter(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var headers = ea.BasicProperties.Headers;

        // Extract death info
        var originalExchange = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathExchange);
        var originalQueue = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathQueue);
        var deathReason = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.FirstDeathReason);
        var eventType = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventType) ?? "unknown";
        var eventId = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventId) ?? "unknown";
        var eventSource = RabbitMqHeaders.GetHeader(headers, RabbitMqHeaders.CloudEventSource) ?? "unknown";
        var routingKey = RabbitMqHeaders.GetHeader(headers, "x-first-death-routing-key") ?? "";

        // Get or initialize retry count
        var retryCount = GetRetryCount(headers);

        Logger.LogWarning(
            "Dead letter received: Type={EventType}, Id={EventId}, Source={Source}, " +
            "Queue={Queue}, Reason={Reason}, RetryCount={RetryCount}/{MaxRetries}",
            eventType, eventId, eventSource, originalQueue, deathReason, retryCount, _maxRetries);

        // Check if we should retry
        if (retryCount < _maxRetries)
        {
            await RetryMessage(ea, retryCount, routingKey, ct);
            return;
        }

        // Max retries exhausted - store permanently
        PermanentlyFailed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

        Logger.LogError(
            "Dead letter exhausted retries: Type={EventType}, Id={EventId}, " +
            "Queue={Queue}, Reason={Reason}",
            eventType, eventId, originalQueue, deathReason);

        // Parse body for storage with size limit and redaction
        JsonElement bodyJson;
        try
        {
            // Prevent JSON bomb attacks with size limit
            if (ea.Body.Length > _maxBodySizeBytes)
            {
                Logger.LogWarning(
                    "Dead letter body exceeds size limit ({BodySize} > {MaxSize}), truncating",
                    ea.Body.Length, _maxBodySizeBytes);
                bodyJson = JsonDocument.Parse("{\"_truncated\": true, \"_reason\": \"body_too_large\"}").RootElement;
            }
            else
            {
                var parsedDoc = JsonDocument.Parse(body);
                bodyJson = RedactSensitiveFields(parsedDoc.RootElement);
            }
        }
        catch (JsonException)
        {
            // Don't store raw body - it might contain binary sensitive data
            bodyJson = JsonDocument.Parse("{\"_raw\": true, \"_reason\": \"non_json_body\"}").RootElement;
        }

        var request = new DeadLetterRequest(
            EventType: eventType,
            EventId: eventId,
            Source: eventSource,
            OriginalQueue: originalQueue ?? "unknown",
            DeathReason: deathReason ?? "unknown",
            RetryCount: retryCount,
            Body: bodyJson,
            ErrorMessage: $"Exhausted {_maxRetries} retries");

        var stored = await eventsApiClient.StoreDeadLetter(request, ct);

        if (stored)
        {
            StorageSuccesses.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        }
        else
        {
            StorageFailures.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
            Logger.LogCritical(
                "ALERT: Failed to store dead letter - DATA LOSS: Type={EventType}, Id={EventId}",
                eventType, eventId);
        }
    }

    private async Task RetryMessage(BasicDeliverEventArgs ea, int currentRetryCount, string routingKey, CancellationToken ct)
    {
        var eventType = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, RabbitMqHeaders.CloudEventType) ?? "unknown";

        RetriesAttempted.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("retry_number", currentRetryCount + 1));

        // Exponential backoff delay
        var delaySeconds = _baseDelaySeconds * (int)Math.Pow(2, currentRetryCount);
        Logger.LogInformation(
            "Scheduling retry {RetryNumber}/{MaxRetries} for event {EventType} in {DelaySeconds}s",
            currentRetryCount + 1, _maxRetries, eventType, delaySeconds);

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);

        // Republish with incremented retry count
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                ["x-dlq-retry-count"] = currentRetryCount + 1
            },
            ContentType = ea.BasicProperties.ContentType,
            DeliveryMode = DeliveryModes.Persistent
        };

        // Copy original headers
        if (ea.BasicProperties.Headers != null)
        {
            foreach (var header in ea.BasicProperties.Headers)
            {
                if (!header.Key.StartsWith("x-death", StringComparison.OrdinalIgnoreCase) &&
                    header.Key != "x-dlq-retry-count")
                {
                    properties.Headers[header.Key] = header.Value;
                }
            }
        }

        var mainExchange = Configuration["RabbitMQ:MainExchange"] ?? "hellarch.events";

        await _publishChannelLock.WaitAsync(ct);
        try
        {
            if (_stopping)
            {
                Logger.LogWarning("Skipping retry - service is stopping");
                return;
            }

            if (_publishChannel is null)
            {
                Logger.LogError("Cannot retry message - publish channel is null");
                return;
            }

            await _publishChannel.BasicPublishAsync(
                exchange: mainExchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body,
                cancellationToken: ct);

            Logger.LogInformation(
                "Retried event {EventType} to {Exchange} with routing key {RoutingKey}",
                eventType, mainExchange, routingKey);
        }
        finally
        {
            _publishChannelLock.Release();
        }
    }

    internal static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers?.TryGetValue("x-dlq-retry-count", out var countObj) == true)
        {
            return countObj switch
            {
                int i => i,
                long l => (int)l,
                byte[] bytes => BitConverter.ToInt32(bytes),
                _ => 0
            };
        }
        return 0;
    }

    /// <summary>
    /// Redacts sensitive fields from a JSON element to prevent PII leakage in dead letter storage.
    /// </summary>
    internal static JsonElement RedactSensitiveFields(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        RedactElement(element, writer);
        writer.Flush();
        stream.Position = 0;
        return JsonDocument.Parse(stream).RootElement;
    }

    private static void RedactElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (SensitiveFields.Contains(property.Name))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        RedactElement(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    RedactElement(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static Activity? StartActivity(BasicDeliverEventArgs ea)
    {
        // Extract trace context
        var traceParent = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, "traceparent");
        var traceState = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, "tracestate");

        ActivityContext parentContext = default;
        if (!string.IsNullOrEmpty(traceParent) && ActivityContext.TryParse(traceParent, traceState, out var parsed))
        {
            parentContext = parsed;
        }

        var eventType = RabbitMqHeaders.GetHeader(ea.BasicProperties.Headers, RabbitMqHeaders.CloudEventType);

        var activity = ActivitySource.StartActivity(
            "dlq.process_dead_letter",
            ActivityKind.Consumer,
            parentContext);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.cloudevents.type", eventType);
        activity?.SetTag("dlq.is_dead_letter", true);

        return activity;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        IChannel? channelToClose;
        await _publishChannelLock.WaitAsync(cancellationToken);
        try
        {
            _stopping = true;
            channelToClose = _publishChannel;
            _publishChannel = null;
        }
        finally
        {
            _publishChannelLock.Release();
        }

        if (channelToClose is not null)
        {
            await channelToClose.CloseAsync(cancellationToken);
            channelToClose.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _publishChannelLock.Dispose();
        base.Dispose();
    }
}
