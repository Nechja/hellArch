namespace HellArch.DataTypes.Auth;

/// <summary>
/// OAuth provider account linked to a user.
/// Stores provider-specific identity info. Token storage is optional
/// (only needed if we want to call provider APIs on behalf of user).
/// </summary>
public class LinkedAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Required: identifies the provider and user
    public required string Provider { get; set; }
    public required string ProviderUserId { get; set; }

    // Optional: provider-specific profile info (varies by provider)
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }

    // Optional: only stored if we need ongoing API access
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
