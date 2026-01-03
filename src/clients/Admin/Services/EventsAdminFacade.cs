using System.Text.Json;
using System.Text.Json.Serialization;
using HellArch.Http;

namespace Admin.Services;

public sealed class EventsAdminFacade(
    IHttpClientFactory httpClientFactory,
    ILogger<EventsAdminFacade> logger)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("EventsService");

    public async Task<AuditLogsResponse> GetAuditLogs(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? actorId = null,
        CancellationToken ct = default)
    {
        var url = $"audit-logs?page={page}&page_size={pageSize}";
        if (!string.IsNullOrEmpty(eventType))
            url += $"&event_type={Uri.EscapeDataString(eventType)}";
        if (!string.IsNullOrEmpty(actorId))
            url += $"&actor_id={Uri.EscapeDataString(actorId)}";

        var response = await _client.GetJsonSafe<AuditLogsResponse>(url, logger, ct);
        return response ?? new AuditLogsResponse([], 0, page, pageSize);
    }

    public async Task<AuditLog?> GetAuditLog(string id, CancellationToken ct = default) =>
        await _client.GetJsonSafe<AuditLog>($"audit-logs/{id}", logger, ct);

    public async Task<WebhooksResponse> GetWebhooks(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await _client.GetJsonSafe<WebhooksResponse>(
            $"webhooks?page={page}&page_size={pageSize}", logger, ct);
        return response ?? new WebhooksResponse([], 0, page, pageSize);
    }

    public async Task<Webhook?> GetWebhook(string id, CancellationToken ct = default) =>
        await _client.GetJsonSafe<Webhook>($"webhooks/{id}", logger, ct);

    public async Task<Webhook?> CreateWebhook(CreateWebhookRequest request, CancellationToken ct = default) =>
        await _client.PostJsonSafe<CreateWebhookRequest, Webhook>("webhooks", request, logger, ct);

    public async Task<Webhook?> UpdateWebhook(string id, UpdateWebhookRequest request, CancellationToken ct = default) =>
        await _client.PostJsonSafe<UpdateWebhookRequest, Webhook>($"webhooks/{id}", request, logger, ct);

    public async Task<bool> DeleteWebhook(string id, CancellationToken ct = default) =>
        await _client.DeleteSafe($"webhooks/{id}", logger, ct);

    public async Task<DeliveriesResponse> GetDeliveries(
        int page = 1,
        int pageSize = 20,
        string? webhookId = null,
        bool? success = null,
        CancellationToken ct = default)
    {
        var url = $"webhook-deliveries?page={page}&page_size={pageSize}";
        if (!string.IsNullOrEmpty(webhookId))
            url += $"&webhook_id={Uri.EscapeDataString(webhookId)}";
        if (success.HasValue)
            url += $"&success={success.Value.ToString().ToLowerInvariant()}";

        var response = await _client.GetJsonSafe<DeliveriesResponse>(url, logger, ct);
        return response ?? new DeliveriesResponse([], 0, page, pageSize);
    }

    public async Task<WebhookDelivery?> GetDelivery(string id, CancellationToken ct = default) =>
        await _client.GetJsonSafe<WebhookDelivery>($"webhook-deliveries/{id}", logger, ct);
}

public sealed record AuditLogsResponse(
    IReadOnlyList<AuditLog> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record AuditLog(
    string Id,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("event_id")] string EventId,
    string Source,
    [property: JsonPropertyName("actor_id")] string? ActorId,
    [property: JsonPropertyName("target_id")] string? TargetId,
    [property: JsonPropertyName("metadata")] JsonElement? Metadata,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record WebhooksResponse(
    IReadOnlyList<Webhook> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

[JsonConverter(typeof(JsonStringEnumConverter<WebhookFormat>))]
public enum WebhookFormat
{
    [JsonStringEnumMemberName("raw")]
    Raw,
    [JsonStringEnumMemberName("discord")]
    Discord,
    [JsonStringEnumMemberName("slack")]
    Slack
}

public sealed record Webhook(
    string Id,
    string Name,
    string Url,
    string Secret,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string> EventTypes,
    [property: JsonPropertyName("format")] WebhookFormat Format,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

public sealed record CreateWebhookRequest(
    string Name,
    string Url,
    string Secret,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string> EventTypes,
    [property: JsonPropertyName("format")] WebhookFormat Format = WebhookFormat.Raw,
    [property: JsonPropertyName("is_active")] bool IsActive = true);

public sealed record UpdateWebhookRequest(
    string? Name = null,
    string? Url = null,
    string? Secret = null,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string>? EventTypes = null,
    [property: JsonPropertyName("format")] WebhookFormat? Format = null,
    [property: JsonPropertyName("is_active")] bool? IsActive = null);

public sealed record DeliveriesResponse(
    IReadOnlyList<WebhookDelivery> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record WebhookDelivery(
    string Id,
    [property: JsonPropertyName("webhook_id")] string WebhookId,
    [property: JsonPropertyName("event_id")] string EventId,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("status_code")] int? StatusCode,
    [property: JsonPropertyName("response_body")] string? ResponseBody,
    bool Success,
    int Attempt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);
