using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

public sealed class RabbitMqAdapter(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<RabbitMqAdapter> logger)
{
    private HttpClient? _client;

    private HttpClient GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        _client = httpClientFactory.CreateClient("RabbitMQ");

        var user = configuration["RABBITMQ_USER"] ?? "hellarch";
        var password = configuration["RABBITMQ_PASSWORD"] ?? "hellarch";
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return _client;
    }

    public async Task<RabbitMqOverview?> GetOverview(CancellationToken ct = default)
    {
        try
        {
            return await GetClient().GetFromJsonAsync<RabbitMqOverview>("api/overview", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting RabbitMQ overview");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting RabbitMQ overview");
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse RabbitMQ overview response");
            return null;
        }
    }

    public async Task<IReadOnlyList<QueueInfo>> GetQueues(CancellationToken ct = default)
    {
        try
        {
            var queues = await GetClient().GetFromJsonAsync<List<QueueInfo>>("api/queues", ct);
            return queues ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting RabbitMQ queues");
            return [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting RabbitMQ queues");
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse RabbitMQ queues response");
            return [];
        }
    }

    public async Task<IReadOnlyList<ExchangeInfo>> GetExchanges(CancellationToken ct = default)
    {
        try
        {
            var exchanges = await GetClient().GetFromJsonAsync<List<ExchangeInfo>>("api/exchanges", ct);
            return exchanges?.Where(e => !string.IsNullOrEmpty(e.Name)).ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting RabbitMQ exchanges");
            return [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting RabbitMQ exchanges");
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse RabbitMQ exchanges response");
            return [];
        }
    }

    public async Task<IReadOnlyList<RabbitMqMessage>> GetMessages(
        string queueName,
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                count,
                ackmode = "ack_requeue_true",
                encoding = "auto"
            };

            var response = await GetClient().PostAsJsonAsync(
                $"api/queues/%2F/{Uri.EscapeDataString(queueName)}/get",
                request,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to get messages from queue {Queue}: {Status}",
                    queueName, response.StatusCode);
                return [];
            }

            var messages = await response.Content.ReadFromJsonAsync<List<RabbitMqMessage>>(ct);
            return messages ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting messages from queue {Queue}", queueName);
            return [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting messages from queue {Queue}", queueName);
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse messages from queue {Queue}", queueName);
            return [];
        }
    }

    public async Task<IReadOnlyList<CloudEventMessage>> GetRecentEvents(
        int count = 20,
        CancellationToken ct = default)
    {
        var messages = await GetMessages("events-worker", count, ct);
        var events = new List<CloudEventMessage>();

        foreach (var msg in messages)
        {
            try
            {
                var cloudEvent = JsonSerializer.Deserialize<CloudEventMessage>(msg.Payload);
                if (cloudEvent != null)
                {
                    cloudEvent.MessageId = msg.Properties?.MessageId;
                    cloudEvent.Timestamp = msg.Properties?.Timestamp;
                    events.Add(cloudEvent);
                }
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Failed to parse message as CloudEvent");
            }
        }

        return events;
    }

    public RabbitMqConnectionInfo GetConnectionInfo()
    {
        var baseAddress = GetClient().BaseAddress?.ToString() ?? "unknown";
        return new RabbitMqConnectionInfo(baseAddress);
    }
}

public sealed record RabbitMqOverview(
    [property: JsonPropertyName("management_version")] string ManagementVersion,
    [property: JsonPropertyName("rates_mode")] string RatesMode,
    [property: JsonPropertyName("cluster_name")] string ClusterName,
    [property: JsonPropertyName("queue_totals")] QueueTotals? QueueTotals,
    [property: JsonPropertyName("object_totals")] ObjectTotals? ObjectTotals,
    [property: JsonPropertyName("message_stats")] MessageStats? MessageStats);

public sealed record QueueTotals(
    [property: JsonPropertyName("messages")] int Messages,
    [property: JsonPropertyName("messages_ready")] int MessagesReady,
    [property: JsonPropertyName("messages_unacknowledged")] int MessagesUnacknowledged);

public sealed record ObjectTotals(
    [property: JsonPropertyName("connections")] int Connections,
    [property: JsonPropertyName("channels")] int Channels,
    [property: JsonPropertyName("exchanges")] int Exchanges,
    [property: JsonPropertyName("queues")] int Queues,
    [property: JsonPropertyName("consumers")] int Consumers);

public sealed record MessageStats(
    [property: JsonPropertyName("publish")] long? Publish,
    [property: JsonPropertyName("deliver")] long? Deliver,
    [property: JsonPropertyName("ack")] long? Ack);

public sealed record QueueInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("messages")] int Messages,
    [property: JsonPropertyName("messages_ready")] int MessagesReady,
    [property: JsonPropertyName("messages_unacknowledged")] int MessagesUnacknowledged,
    [property: JsonPropertyName("consumers")] int Consumers,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("durable")] bool Durable);

public sealed record ExchangeInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("durable")] bool Durable,
    [property: JsonPropertyName("auto_delete")] bool AutoDelete);

public sealed record RabbitMqMessage(
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("payload_encoding")] string PayloadEncoding,
    [property: JsonPropertyName("properties")] MessageProperties? Properties);

public sealed record MessageProperties(
    [property: JsonPropertyName("message_id")] string? MessageId,
    [property: JsonPropertyName("timestamp")] long? Timestamp,
    [property: JsonPropertyName("content_type")] string? ContentType);

public sealed class CloudEventMessage
{
    [JsonPropertyName("specversion")]
    public string? SpecVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonIgnore]
    public string? MessageId { get; set; }

    [JsonIgnore]
    public long? Timestamp { get; set; }

    public string GetEventTypeName() => Type?.Split('.').LastOrDefault()?.Replace(".v1", "") ?? "unknown";

    public string GetFormattedData() => Data?.ToString() ?? "{}";
}

public sealed record RabbitMqConnectionInfo(string ManagementUrl);
