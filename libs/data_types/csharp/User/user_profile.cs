namespace HellArch.DataTypes.User
{

    /// <summary>
    /// Extended user profile info
    /// </summary>
    public partial class UserProfile
    {
        public Guid AuthUserId { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string DisplayName { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
