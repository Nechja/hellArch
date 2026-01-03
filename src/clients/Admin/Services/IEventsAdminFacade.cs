namespace Admin.Services;

/// <summary>
/// Abstraction for events administration operations.
/// </summary>
public interface IEventsAdminFacade
{
    Task<AuditLogsResponse> GetAuditLogs(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? actorId = null,
        CancellationToken ct = default);

    Task<AuditLog?> GetAuditLog(string id, CancellationToken ct = default);

    Task<WebhooksResponse> GetWebhooks(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<Webhook?> GetWebhook(string id, CancellationToken ct = default);
    Task<Webhook?> CreateWebhook(CreateWebhookRequest request, CancellationToken ct = default);
    Task<Webhook?> UpdateWebhook(string id, UpdateWebhookRequest request, CancellationToken ct = default);
    Task<bool> DeleteWebhook(string id, CancellationToken ct = default);

    Task<DeliveriesResponse> GetDeliveries(
        int page = 1,
        int pageSize = 20,
        string? webhookId = null,
        bool? success = null,
        CancellationToken ct = default);

    Task<WebhookDelivery?> GetDelivery(string id, CancellationToken ct = default);
}
