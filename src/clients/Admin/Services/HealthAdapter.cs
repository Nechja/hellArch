using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

public class HealthAdapter(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("HealthService");

    public async Task<AggregatedStatus?> GetStatus()
    {
        try
        {
            return await _client.GetFromJsonAsync<AggregatedStatus>("status");
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<ServiceStatus?> GetServiceStatus(string name)
    {
        try
        {
            return await _client.GetFromJsonAsync<ServiceStatus>($"status/{name}");
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<WorkersStatus?> GetWorkers()
    {
        try
        {
            return await _client.GetFromJsonAsync<WorkersStatus>("workers");
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
    }
}

public record AggregatedStatus(
    [property: JsonPropertyName("overall")] string Overall,
    [property: JsonPropertyName("checked_at")] string CheckedAt,
    [property: JsonPropertyName("services")] Dictionary<string, ServiceHealthInfo> Services
);

public record ServiceHealthInfo(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("latency_ms")] int LatencyMs,
    [property: JsonPropertyName("last_check")] string LastCheck,
    [property: JsonPropertyName("error")] string? Error
);

public record ServiceStatus(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("latency_ms")] int LatencyMs,
    [property: JsonPropertyName("last_check")] string LastCheck,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("history")] IReadOnlyList<HealthCheck> History
);

public record HealthCheck(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("latency_ms")] int LatencyMs,
    [property: JsonPropertyName("checked_at")] string CheckedAt,
    [property: JsonPropertyName("error")] string? Error
);

public record WorkersStatus(
    [property: JsonPropertyName("overall")] string Overall,
    [property: JsonPropertyName("workers")] IReadOnlyList<WorkerInfo> Workers
);

public record WorkerInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("queue")] string Queue,
    [property: JsonPropertyName("consumers")] int Consumers,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("messages")] int Messages
);

