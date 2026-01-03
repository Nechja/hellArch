using System.Text;

namespace HellArch.RabbitMQ;

/// <summary>
/// Utilities for working with RabbitMQ message headers.
/// </summary>
public static class RabbitMqHeaders
{
    /// <summary>
    /// CloudEvents header prefix.
    /// </summary>
    public const string CloudEventPrefix = "ce-";

    /// <summary>
    /// CloudEvents type header.
    /// </summary>
    public const string CloudEventType = "ce-type";

    /// <summary>
    /// CloudEvents id header.
    /// </summary>
    public const string CloudEventId = "ce-id";

    /// <summary>
    /// CloudEvents source header.
    /// </summary>
    public const string CloudEventSource = "ce-source";

    /// <summary>
    /// Dead letter exchange header (set by RabbitMQ on first death).
    /// </summary>
    public const string FirstDeathExchange = "x-first-death-exchange";

    /// <summary>
    /// Dead letter queue header (set by RabbitMQ on first death).
    /// </summary>
    public const string FirstDeathQueue = "x-first-death-queue";

    /// <summary>
    /// Dead letter reason header (set by RabbitMQ on first death).
    /// </summary>
    public const string FirstDeathReason = "x-first-death-reason";

    /// <summary>
    /// Extracts a string value from RabbitMQ headers.
    /// Handles byte arrays, strings, and null values.
    /// </summary>
    public static string? GetHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        if (!headers.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string s => s,
            null => null,
            _ => value.ToString()
        };
    }
}
