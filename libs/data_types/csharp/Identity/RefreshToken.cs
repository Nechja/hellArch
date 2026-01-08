namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 refresh token (long-lived, 30 days).
/// Stored as SHA-256 hash for security.
/// Supports token rotation with reuse detection.
/// </summary>
public class RefreshToken
{
    public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public required string ClientId { get; set; }
    public required string Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Token family ID for rotation tracking.
    /// All tokens in a family share this ID.
    /// If a revoked token is reused, entire family is revoked.
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// Whether this token has been used/revoked.
    /// Reuse of a revoked token triggers family revocation.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// SHA-256 hash of client fingerprint (IP + User-Agent).
    /// Token is rejected if fingerprint doesn't match.
    /// </summary>
    public string? ClientFingerprint { get; set; }

    // Navigation
    public IdentityUser? User { get; set; }
}
