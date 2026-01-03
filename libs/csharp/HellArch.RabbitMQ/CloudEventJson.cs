using System.Text.Json;

namespace HellArch.RabbitMQ;

/// <summary>
/// JSON serialization options for CloudEvents.
/// </summary>
public static class CloudEventJson
{
    /// <summary>
    /// Default JSON serializer options for CloudEvents.
    /// Case-insensitive property names with camelCase naming policy.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
