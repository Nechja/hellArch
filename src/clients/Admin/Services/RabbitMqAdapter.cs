using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Admin.Services;

public sealed class RabbitMqAdapter(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<RabbitMqAdapter> logger) : IRabbitMqAdapter
{
    private readonly Lazy<HttpClient> _client = new(() =>
    {
        var client = httpClientFactory.CreateClient("RabbitMQ");
        var user = configuration["RABBITMQ_USER"] ?? "hellarch";
        var password = configuration["RABBITMQ_PASSWORD"] ?? "hellarch";
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    });

    private HttpClient Client => _client.Value;

    public async Task<RabbitMqOverview?> GetOverview(CancellationToken ct = default)
    {
        try
        {
            return await Client.GetFromJsonAsync<RabbitMqOverview>("api/overview", ct);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<IReadOnlyList<QueueInfo>> GetQueues(CancellationToken ct = default)
    {
        try
        {
            return await Client.GetFromJsonAsync<List<QueueInfo>>("api/queues", ct) ?? [];
        }
        catch (HttpRequestException) { return []; }
        catch (TaskCanceledException) { return []; }
        catch (JsonException) { return []; }
    }

    public async Task<IReadOnlyList<ExchangeInfo>> GetExchanges(CancellationToken ct = default)
    {
        try
        {
            var exchanges = await Client.GetFromJsonAsync<List<ExchangeInfo>>("api/exchanges", ct);
            return exchanges?.Where(e => !string.IsNullOrEmpty(e.Name)).ToList() ?? [];
        }
        catch (HttpRequestException) { return []; }
        catch (TaskCanceledException) { return []; }
        catch (JsonException) { return []; }
    }

    public async Task<IReadOnlyList<RabbitMqMessage>> GetMessages(
        string queueName,
        int count = 10,
        CancellationToken ct = default)
    {
        try
        {
            var request = new { count, ackmode = "ack_requeue_true", encoding = "auto" };
            var response = await Client.PostAsJsonAsync(
                $"api/queues/%2F/{Uri.EscapeDataString(queueName)}/get",
                request,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<RabbitMqMessage>>(ct) ?? [];
        }
        catch (HttpRequestException) { return []; }
        catch (TaskCanceledException) { return []; }
        catch (JsonException) { return []; }
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
        var baseAddress = Client.BaseAddress?.ToString() ?? "unknown";
        return new RabbitMqConnectionInfo(baseAddress);
    }
}
