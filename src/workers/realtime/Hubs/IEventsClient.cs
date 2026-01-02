using System.Text.Json;

namespace Realtime.Hubs;

/// <summary>
/// Typed client interface for SignalR hub.
/// Defines methods the server can invoke on connected clients.
/// </summary>
public interface IEventsClient
{
    /// <summary>
    /// Receives a CloudEvent. This is the primary method for pushing events to clients.
    /// </summary>
    Task ReceiveEvent(CloudEventDto cloudEvent);

    /// <summary>
    /// Receives a notification that a session was invalidated.
    /// Clients should handle this by redirecting to login.
    /// </summary>
    Task SessionInvalidated(string reason);

    /// <summary>
    /// Receives a notification that user data was updated.
    /// Clients should refresh their cached user data.
    /// </summary>
    Task UserUpdated();
}

/// <summary>
/// CloudEvent DTO for SignalR transmission.
/// Matches the CloudEvents 1.0 specification.
/// </summary>
public sealed record CloudEventDto
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset Time { get; init; }
    public JsonElement? Data { get; init; }

    /// <summary>
    /// Extracts user_id from the event data if present.
    /// </summary>
    public string? GetUserId()
    {
        if (Data is not { } data)
        {
            return null;
        }

        if (data.TryGetProperty("user_id", out var userId))
        {
            return userId.GetString();
        }

        return null;
    }
}
