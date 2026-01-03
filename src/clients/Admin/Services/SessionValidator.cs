using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

/// <summary>
/// Default implementation of ISessionValidator that validates sessions
/// against the auth service via HTTP.
/// </summary>
public sealed class SessionValidator(
    IHttpClientFactory httpClientFactory,
    ILogger<SessionValidator> logger) : ISessionValidator
{
    private const string AuthServiceName = "AuthService";

    public async Task<SessionValidationResult?> Validate(string sessionToken, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(AuthServiceName);
            client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

            var response = await client.PostAsync("auth/validate", null, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Session validation failed with status {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ValidateResponseDto>(ct);

            if (result is null || !result.Valid)
            {
                logger.LogDebug("Session is not valid");
                return null;
            }

            return new SessionValidationResult(
                result.Valid,
                result.UserId,
                result.Email,
                result.Username,
                result.Permissions);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error validating session");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Session validation timed out");
            return null;
        }
    }

    private sealed record ValidateResponseDto(
        bool Valid,
        [property: JsonPropertyName("user_id")] string? UserId,
        string? Email,
        string? Username,
        Dictionary<string, bool>? Permissions);
}
