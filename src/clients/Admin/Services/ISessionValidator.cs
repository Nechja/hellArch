namespace Admin.Services;

/// <summary>
/// Abstraction for validating user sessions against the auth service.
/// </summary>
public interface ISessionValidator
{
    /// <summary>
    /// Validates a session token and returns the session details if valid.
    /// </summary>
    Task<SessionValidationResult?> Validate(string sessionToken, CancellationToken ct = default);
}

/// <summary>
/// Result of session validation containing user information.
/// </summary>
public sealed record SessionValidationResult(
    bool Valid,
    string? UserId,
    string? Email,
    string? Username,
    Dictionary<string, bool>? Permissions);
