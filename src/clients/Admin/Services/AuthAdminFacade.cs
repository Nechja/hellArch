using System.Text.Json.Serialization;
using HellArch.Http;

namespace Admin.Services;

public sealed class AuthAdminFacade(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthAdminFacade> logger)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("AuthService");

    public async Task<UsersResponse> GetUsers(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await _client.GetJsonSafe<UsersResponse>(
            $"auth/admin/users?page={page}&pageSize={pageSize}", logger, ct);
        return response ?? new UsersResponse([], 0, page, pageSize);
    }

    public async Task<UserDetail?> GetUser(string userId, CancellationToken ct = default) =>
        await _client.GetJsonSafe<UserDetail>($"auth/admin/users/{userId}", logger, ct);

    public async Task<IReadOnlyList<SessionInfo>> GetUserSessions(string userId, CancellationToken ct = default)
    {
        var response = await _client.GetJsonSafe<SessionsResponse>(
            $"auth/admin/users/{userId}/sessions", logger, ct);
        return response?.Sessions ?? [];
    }

    public async Task<bool> RevokeSession(string sessionId, CancellationToken ct = default) =>
        await _client.DeleteSafe($"auth/admin/sessions/{sessionId}", logger, ct);

    public async Task<Dictionary<string, bool>> GetUserPermissions(string userId, CancellationToken ct = default)
    {
        var response = await _client.GetJsonSafe<PermissionsResponse>(
            $"auth/admin/users/{userId}/permissions", logger, ct);
        return response?.Permissions ?? [];
    }

    public async Task<bool> SetUserPermissions(string userId, Dictionary<string, bool> permissions, CancellationToken ct = default) =>
        await _client.PutJsonSafe($"auth/admin/users/{userId}/permissions", new { permissions }, logger, ct);

    public async Task<bool> GrantPermission(string userId, string permission, CancellationToken ct = default) =>
        await _client.PostJsonSafe($"auth/admin/users/{userId}/permissions/grant", new { permission }, logger, ct);

    public async Task<bool> RevokePermission(string userId, string permission, CancellationToken ct = default) =>
        await _client.PostJsonSafe($"auth/admin/users/{userId}/permissions/revoke", new { permission }, logger, ct);

    public async Task<bool> ActivateUser(string userId, CancellationToken ct = default) =>
        await _client.PostJsonSafe($"auth/admin/users/{userId}/activate", new { }, logger, ct);

    public async Task<bool> DeactivateUser(string userId, CancellationToken ct = default) =>
        await _client.PostJsonSafe($"auth/admin/users/{userId}/deactivate", new { }, logger, ct);

    public async Task<AdminStats> GetStats(CancellationToken ct = default)
    {
        var response = await _client.GetJsonSafe<AdminStats>("auth/admin/stats", logger, ct);
        return response ?? new AdminStats(0, 0, 0);
    }
}

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
