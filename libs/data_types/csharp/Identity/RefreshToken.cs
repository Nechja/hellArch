namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 refresh token (long-lived, 30 days).
/// Stored as SHA-256 hash for security.
/// </summary>
public class RefreshToken
{
    public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public required string ClientId { get; set; }
    public required string Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public IdentityUser? User { get; set; }
}
