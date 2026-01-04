namespace Admin.Services;

/// <summary>
/// Abstraction for auth administration operations.
/// </summary>
public interface IAuthAdminFacade
{
    Task<UsersResponse> GetUsers(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<UserDetail?> GetUser(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionInfo>> GetUserSessions(string userId, CancellationToken ct = default);
    Task<bool> RevokeSession(string sessionId, CancellationToken ct = default);
    Task<Dictionary<string, bool>> GetUserPermissions(string userId, CancellationToken ct = default);
    Task<bool> SetUserPermissions(string userId, Dictionary<string, bool> permissions, CancellationToken ct = default);
    Task<bool> GrantPermission(string userId, string permission, CancellationToken ct = default);
    Task<bool> RevokePermission(string userId, string permission, CancellationToken ct = default);
    Task<bool> ActivateUser(string userId, CancellationToken ct = default);
    Task<bool> DeactivateUser(string userId, CancellationToken ct = default);
    Task<AdminStats> GetStats(CancellationToken ct = default);
}
