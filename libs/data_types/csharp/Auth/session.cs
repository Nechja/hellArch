namespace HellArch.DataTypes.Auth
{

    /// <summary>
    /// User session for cookie-based auth
    /// </summary>
    public partial class Session
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public Guid Id { get; set; }
        public string Token { get; set; }
        public Guid UserId { get; set; }
    }
}
