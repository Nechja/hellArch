namespace HellArch.DataTypes.Auth;

/// <summary>
/// Core user identity in the auth system.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<LinkedAccount> LinkedAccounts { get; set; } = [];
}
