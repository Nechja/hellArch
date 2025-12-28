namespace HellArch.CloudEvents.Auth;

/// <summary>
/// Published when a new session is created.
/// Event type: hellarch.auth.session.created.v1
/// </summary>
public class SessionCreated
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Published when a session ends (logout or expiry).
/// Event type: hellarch.auth.session.ended.v1
/// </summary>
public class SessionEnded
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required SessionEndReason Reason { get; init; }
    public required DateTimeOffset EndedAt { get; init; }
}

public enum SessionEndReason
{
    Logout,
    Expired,
    Revoked
}
