namespace HellArch.DataTypes.User
{

    /// <summary>
    /// User preferences and settings
    /// </summary>
    public partial class UserPreferences
    {
        public DateTimeOffset CreatedAt { get; set; }
        public bool EmailNotifications { get; set; }
        public Guid Id { get; set; }
        public string Locale { get; set; }
        public Theme Theme { get; set; }
        public string Timezone { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UserProfileId { get; set; }
    }

    public enum Theme { Dark, Light, System };
}
