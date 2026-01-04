namespace HellArch.DataTypes.Identity;

/// <summary>
/// OAuth 2.0 client application registration.
/// </summary>
public class OAuthClient
{
    public required string Id { get; set; }
    public required string SecretHash { get; set; }
    public required string Name { get; set; }
    public required string[] RedirectUris { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
