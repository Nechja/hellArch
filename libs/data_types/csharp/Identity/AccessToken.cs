namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 access token (short-lived, 1 hour).
/// Stored for validation until expiry.
/// </summary>
public class AccessToken
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
