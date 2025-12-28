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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get users");
            return new UsersResponse([], 0, page, pageSize);
        }
    }

    public async Task<UserDetail?> GetUser(string userId, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetFromJsonAsync<UserDetail>($"auth/admin/users/{userId}", ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get user {UserId}", userId);
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get sessions for user {UserId}", userId);
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to revoke session {SessionId}", sessionId);
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get stats");
            return new AdminStats(0, 0, 0);
        }
    }
}

// DTOs for Auth Admin API responses
public sealed record UsersResponse(
    IReadOnlyList<UserSummary> Users,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record UserSummary(
    string Id,
    string Email,
    string Username,
    DateTime CreatedAt);

public sealed record UserDetail(
    string Id,
    string Email,
    string Username,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<LinkedAccountInfo> LinkedAccounts);

public sealed record LinkedAccountInfo(
    string Id,
    string Provider,
    string ProviderUserId,
    string? Email,
    string? Username,
    string? AvatarUrl,
    DateTime CreatedAt);

public sealed record SessionsResponse(
    IReadOnlyList<SessionInfo> Sessions);

public sealed record SessionInfo(
    string Id,
    string UserId,
    string? IpAddress,
    string? UserAgent,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public sealed record AdminStats(
    [property: JsonPropertyName("user_count")] int UserCount,
    [property: JsonPropertyName("session_count")] int SessionCount,
    [property: JsonPropertyName("linked_account_count")] int LinkedAccountCount);
