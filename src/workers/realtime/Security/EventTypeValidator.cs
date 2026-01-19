using System.Text.RegularExpressions;

namespace Realtime.Security;

/// <summary>
/// Validates event type names to prevent group name injection attacks.
/// </summary>
public static partial class EventTypeValidator
{
    /// <summary>
    /// Valid event type pattern: starts with letter, alphanumeric with dots/underscores/hyphens, max 100 chars.
    /// This prevents injection of special characters that could manipulate SignalR group names.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._-]{0,99}$")]
    private static partial Regex EventTypePattern();

    /// <summary>
    /// Validates that an event type name is safe to use as a SignalR group suffix.
    /// </summary>
    /// <param name="eventType">The event type to validate</param>
    /// <returns>True if the event type is valid, false otherwise</returns>
    public static bool IsValid(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return false;
        }

        return EventTypePattern().IsMatch(eventType);
    }
}
