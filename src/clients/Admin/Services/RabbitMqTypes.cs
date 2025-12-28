using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

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
