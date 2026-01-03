using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace DlqWorker.Services;

/// <summary>
/// HTTP client for interacting with the Events API to store dead letters.
/// </summary>
public sealed class EventsApiClient(HttpClient httpClient, ILogger<EventsApiClient> logger)
{
    private static readonly ActivitySource ActivitySource = new("dlq-worker");

    public async Task<bool> StoreDeadLetter(DeadLetterRequest request, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("dlq.store_dead_letter");
        activity?.SetTag("event.type", request.EventType);
        activity?.SetTag("event.id", request.EventId);

        try
        {
            var response = await httpClient.PostAsJsonAsync("/dead-letters", request, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Stored dead letter: Type={EventType}, Id={EventId}",
                    request.EventType,
                    request.EventId);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Failed to store dead letter: Status={Status}, Body={Body}",
                response.StatusCode,
                errorBody);
            activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error storing dead letter");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return false;
        }
    }
}

public sealed record DeadLetterRequest(
    string EventType,
    string EventId,
    string Source,
    string OriginalQueue,
    string DeathReason,
    int RetryCount,
    JsonElement Body,
    string? ErrorMessage);
