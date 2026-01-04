namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 authorization code grant (short-lived, 5 minutes).
/// </summary>
public class AuthorizationCode
{
    public required string Code { get; set; }
    public required string ClientId { get; set; }
    public Guid UserId { get; set; }
    public required string RedirectUri { get; set; }
    public required string Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public IdentityUser? User { get; set; }
}
