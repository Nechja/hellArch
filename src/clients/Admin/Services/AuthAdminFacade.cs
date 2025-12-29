using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Services;

public sealed class AuthAdminFacade(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthAdminFacade> logger)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("AuthService");

    public async Task<UsersResponse> GetUsers(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<UsersResponse>(
                $"auth/admin/users?page={page}&pageSize={pageSize}", ct);
            return response ?? new UsersResponse([], 0, page, pageSize);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting users");
            return new UsersResponse([], 0, page, pageSize);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting users");
            return new UsersResponse([], 0, page, pageSize);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse users response");
            return new UsersResponse([], 0, page, pageSize);
        }
    }

    public async Task<UserDetail?> GetUser(string userId, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<UserDetail>($"auth/admin/users/{userId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting user {UserId}", userId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting user {UserId}", userId);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse user response for {UserId}", userId);
            return null;
        }
    }

    public async Task<IReadOnlyList<SessionInfo>> GetUserSessions(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<SessionsResponse>(
                $"auth/admin/users/{userId}/sessions", ct);
            return response?.Sessions ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting sessions for user {UserId}", userId);
            return [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting sessions for user {UserId}", userId);
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse sessions response for user {UserId}", userId);
            return [];
        }
    }

    public async Task<bool> RevokeSession(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync($"auth/admin/sessions/{sessionId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error revoking session {SessionId}", sessionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout revoking session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> GetUserPermissions(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<PermissionsResponse>(
                $"auth/admin/users/{userId}/permissions", ct);
            return response?.Permissions ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting permissions for user {UserId}", userId);
            return [];
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting permissions for user {UserId}", userId);
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse permissions response for user {UserId}", userId);
            return [];
        }
    }

    public async Task<bool> SetUserPermissions(string userId, Dictionary<string, bool> permissions, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PutAsJsonAsync(
                $"auth/admin/users/{userId}/permissions",
                new { permissions },
                ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error setting permissions for user {UserId}", userId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout setting permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> GrantPermission(string userId, string permission, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(
                $"auth/admin/users/{userId}/permissions/grant",
                new { permission },
                ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error granting permission {Permission} to user {UserId}", permission, userId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout granting permission {Permission} to user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> RevokePermission(string userId, string permission, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(
                $"auth/admin/users/{userId}/permissions/revoke",
                new { permission },
                ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error revoking permission {Permission} from user {UserId}", permission, userId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout revoking permission {Permission} from user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> ActivateUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsync($"auth/admin/users/{userId}/activate", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error activating user {UserId}", userId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout activating user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeactivateUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsync($"auth/admin/users/{userId}/deactivate", null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error deactivating user {UserId}", userId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout deactivating user {UserId}", userId);
            return false;
        }
    }

    public async Task<AdminStats> GetStats(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<AdminStats>("auth/admin/stats", ct);
            return response ?? new AdminStats(0, 0, 0);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error getting stats");
            return new AdminStats(0, 0, 0);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timeout getting stats");
            return new AdminStats(0, 0, 0);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse stats response");
            return new AdminStats(0, 0, 0);
        }
    }
}

// DTOs for Auth Admin API responses
public sealed record UsersResponse(
    IReadOnlyList<UserSummary> Users,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record UserSummary(
    string Id,
    string Email,
    string Username,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record UserDetail(
    string Id,
    string Email,
    string Username,
    [property: JsonPropertyName("is_active")] bool IsActive,
    Dictionary<string, bool>? Permissions,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("linked_accounts")] IReadOnlyList<LinkedAccountInfo> LinkedAccounts);

public sealed record PermissionsResponse(
    [property: JsonPropertyName("user_id")] string UserId,
    Dictionary<string, bool> Permissions);

public sealed record LinkedAccountInfo(
    string Id,
    string Provider,
    [property: JsonPropertyName("provider_user_id")] string ProviderUserId,
    string? Email,
    string? Username,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record SessionsResponse(
    IReadOnlyList<SessionInfo> Sessions);

public sealed record SessionInfo(
    string Id,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("ip_address")] string? IpAddress,
    [property: JsonPropertyName("user_agent")] string? UserAgent,
    [property: JsonPropertyName("expires_at")] DateTime ExpiresAt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record AdminStats(
    [property: JsonPropertyName("user_count")] int UserCount,
    [property: JsonPropertyName("session_count")] int SessionCount,
    [property: JsonPropertyName("linked_account_count")] int LinkedAccountCount);
