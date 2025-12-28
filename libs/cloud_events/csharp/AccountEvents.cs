namespace HellArch.CloudEvents.Auth;

/// <summary>
/// Published when an OAuth account is linked to a user.
/// Event type: hellarch.auth.account.linked.v1
/// </summary>
public class AccountLinked
{
    public required string UserId { get; init; }
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset LinkedAt { get; init; }
}

/// <summary>
/// Published when an OAuth account is unlinked from a user.
/// Event type: hellarch.auth.account.unlinked.v1
/// </summary>
public class AccountUnlinked
{
    public required string UserId { get; init; }
    public required string Provider { get; init; }
    public required DateTimeOffset UnlinkedAt { get; init; }
}
