namespace HellArch.DataTypes.User;

/// <summary>
/// User preferences and settings.
/// </summary>
public class UserPreferences
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }

    public Theme Theme { get; set; } = Theme.System;
    public string? Locale { get; set; }
    public string? Timezone { get; set; }

    // Notification settings
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties (EF Core)
    public UserProfile? UserProfile { get; set; }
}

public enum Theme
{
    System,
    Light,
    Dark
}
