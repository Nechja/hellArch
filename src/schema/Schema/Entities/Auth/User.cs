namespace HellArch.DataTypes.Auth;

public partial class User
{
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<LinkedAccount> LinkedAccounts { get; set; } = [];
}
