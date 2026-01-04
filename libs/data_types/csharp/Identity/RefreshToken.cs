namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 refresh token (long-lived, 30 days).
/// </summary>
public class RefreshToken
{
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public required string ClientId { get; set; }
    public required string Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public IdentityUser? User { get; set; }
}
