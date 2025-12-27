namespace HellArch.DataTypes.Auth
{

    /// <summary>
    /// Core user identity
    /// </summary>
    public partial class User
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string Email { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Username { get; set; }
    }
}
