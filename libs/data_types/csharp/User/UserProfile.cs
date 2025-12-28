namespace HellArch.DataTypes.User;

/// <summary>
/// Extended user profile information.
/// Linked to auth user via AuthUserId.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }

    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
