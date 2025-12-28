namespace HellArch.CloudEvents.Auth;

/// <summary>
/// Published when a new user is created via OAuth.
/// Event type: hellarch.auth.user.created.v1
/// </summary>
public class UserCreated
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public required string AvatarUrl { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Published when user info is updated.
/// Event type: hellarch.auth.user.updated.v1
/// </summary>
public class UserUpdated
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Published when a user is deleted.
/// Event type: hellarch.auth.user.deleted.v1
/// </summary>
public class UserDeleted
{
    public required string UserId { get; init; }
    public required DateTimeOffset DeletedAt { get; init; }
}
