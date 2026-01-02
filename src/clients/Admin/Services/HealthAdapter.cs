using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

public class HealthAdapter(IHttpClientFactory httpClientFactory, ILogger<HealthAdapter> logger)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("HealthService");

    public async Task<AggregatedStatus?> GetStatus()
    {
        try
        {
            var response = await _client.GetAsync("status");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Health service returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AggregatedStatus>(json, JsonOptions.Default);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to connect to health service");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Health service request timed out");
            return null;
        }
    }

    public async Task<ServiceStatus?> GetServiceStatus(string name)
    {
        try
        {
            var response = await _client.GetAsync($"status/{name}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServiceStatus>(json, JsonOptions.Default);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to get status for service {Name}", name);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timed out for service {Name}", name);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse response for service {Name}", name);
            return null;
        }
    }

    public async Task<WorkersStatus?> GetWorkers()
    {
        try
        {
            var response = await _client.GetAsync("workers");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Health service workers endpoint returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WorkersStatus>(json, JsonOptions.Default);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to get workers status");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Workers status request timed out");
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse workers response");
            return null;
        }
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

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
