using System.Text.Json;

namespace HellArch.CloudEvents;

/// <summary>
/// CloudEvent DTO following the CloudEvents 1.0 specification.
/// Used for JSON serialization/deserialization of CloudEvents.
/// </summary>
public sealed record CloudEventDto
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// URI identifying the event source.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Event type identifier (e.g., "hellarch.auth.user.created.v1").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public required DateTimeOffset Time { get; init; }

    /// <summary>
    /// Event payload as a JSON element.
    /// </summary>
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

    /// <summary>
    /// Gets the short event type name (without "hellarch." prefix).
    /// </summary>
    public string GetShortTypeName()
    {
        var parts = Type.Split('.');
        return parts.Length > 1 ? string.Join(".", parts[1..]) : Type;
    }
}
