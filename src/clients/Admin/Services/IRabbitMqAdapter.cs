namespace Admin.Services;

/// <summary>
/// Abstraction for RabbitMQ management API operations.
/// </summary>
public interface IRabbitMqAdapter
{
    Task<RabbitMqOverview?> GetOverview(CancellationToken ct = default);
    Task<IReadOnlyList<QueueInfo>> GetQueues(CancellationToken ct = default);
    Task<IReadOnlyList<ExchangeInfo>> GetExchanges(CancellationToken ct = default);
    Task<IReadOnlyList<RabbitMqMessage>> GetMessages(string queueName, int count = 10, CancellationToken ct = default);
    Task<IReadOnlyList<CloudEventMessage>> GetRecentEvents(int count = 20, CancellationToken ct = default);
    RabbitMqConnectionInfo GetConnectionInfo();
}
