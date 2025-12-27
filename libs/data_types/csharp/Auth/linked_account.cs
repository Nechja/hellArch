namespace HellArch.DataTypes.Auth
{

    /// <summary>
    /// OAuth provider linked to a user
    /// </summary>
    public partial class LinkedAccount
    {
        public string AccessToken { get; set; }
        public string AvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Email { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public Guid Id { get; set; }
        public string Provider { get; set; }
        public string ProviderUserId { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}
