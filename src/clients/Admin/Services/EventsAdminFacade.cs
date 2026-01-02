using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

public sealed class EventsAdminFacade(
    IHttpClientFactory httpClientFactory,
    ILogger<EventsAdminFacade> logger)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("EventsService");

    // Audit Logs

    public async Task<AuditLogsResponse> GetAuditLogs(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? actorId = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/audit-logs?page={page}&page_size={pageSize}";
            if (!string.IsNullOrEmpty(eventType))
            {
                url += $"&event_type={Uri.EscapeDataString(eventType)}";
            }
            if (!string.IsNullOrEmpty(actorId))
            {
                url += $"&actor_id={Uri.EscapeDataString(actorId)}";
            }

            var response = await _client.GetFromJsonAsync<AuditLogsResponse>(url, ct);
            return response ?? new AuditLogsResponse([], 0, page, pageSize);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting audit logs");
            return new AuditLogsResponse([], 0, page, pageSize);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting audit logs");
            return new AuditLogsResponse([], 0, page, pageSize);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse audit logs response");
            return new AuditLogsResponse([], 0, page, pageSize);
        }
    }

    public async Task<AuditLog?> GetAuditLog(string id, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<AuditLog>($"api/audit-logs/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting audit log {Id}", id);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting audit log {Id}", id);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse audit log response for {Id}", id);
            return null;
        }
    }

    // Webhooks

    public async Task<WebhooksResponse> GetWebhooks(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<WebhooksResponse>(
                $"api/webhooks?page={page}&page_size={pageSize}", ct);
            return response ?? new WebhooksResponse([], 0, page, pageSize);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting webhooks");
            return new WebhooksResponse([], 0, page, pageSize);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting webhooks");
            return new WebhooksResponse([], 0, page, pageSize);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse webhooks response");
            return new WebhooksResponse([], 0, page, pageSize);
        }
    }

    public async Task<Webhook?> GetWebhook(string id, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<Webhook>($"api/webhooks/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting webhook {Id}", id);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting webhook {Id}", id);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse webhook response for {Id}", id);
            return null;
        }
    }

    public async Task<Webhook?> CreateWebhook(CreateWebhookRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync("api/webhooks", request, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Webhook>(ct);
            }
            logger.LogWarning("Failed to create webhook: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error creating webhook");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout creating webhook");
            return null;
        }
    }

    public async Task<Webhook?> UpdateWebhook(string id, UpdateWebhookRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PutAsJsonAsync($"api/webhooks/{id}", request, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Webhook>(ct);
            }
            logger.LogWarning("Failed to update webhook {Id}: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error updating webhook {Id}", id);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout updating webhook {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteWebhook(string id, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync($"api/webhooks/{id}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error deleting webhook {Id}", id);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout deleting webhook {Id}", id);
            return false;
        }
    }

    // Webhook Deliveries

    public async Task<DeliveriesResponse> GetDeliveries(
        int page = 1,
        int pageSize = 20,
        string? webhookId = null,
        bool? success = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/webhook-deliveries?page={page}&page_size={pageSize}";
            if (!string.IsNullOrEmpty(webhookId))
            {
                url += $"&webhook_id={Uri.EscapeDataString(webhookId)}";
            }
            if (success.HasValue)
            {
                url += $"&success={success.Value.ToString().ToLowerInvariant()}";
            }

            var response = await _client.GetFromJsonAsync<DeliveriesResponse>(url, ct);
            return response ?? new DeliveriesResponse([], 0, page, pageSize);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting deliveries");
            return new DeliveriesResponse([], 0, page, pageSize);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting deliveries");
            return new DeliveriesResponse([], 0, page, pageSize);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse deliveries response");
            return new DeliveriesResponse([], 0, page, pageSize);
        }
    }

    public async Task<WebhookDelivery?> GetDelivery(string id, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<WebhookDelivery>($"api/webhook-deliveries/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting delivery {Id}", id);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting delivery {Id}", id);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse delivery response for {Id}", id);
            return null;
        }
    }
}

// DTOs for Events API responses

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

public sealed record Webhook(
    string Id,
    string Name,
    string Url,
    string Secret,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string> EventTypes,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

public sealed record CreateWebhookRequest(
    string Name,
    string Url,
    string Secret,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string> EventTypes,
    [property: JsonPropertyName("is_active")] bool IsActive = true);

public sealed record UpdateWebhookRequest(
    string? Name = null,
    string? Url = null,
    string? Secret = null,
    [property: JsonPropertyName("event_types")] IReadOnlyList<string>? EventTypes = null,
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
