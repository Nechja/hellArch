namespace HellArch.DataTypes.Identity

open System

/// Local identity user with username/password authentication.
type IdentityUser = {
    Id: Guid
    Email: string
    Username: string
    PasswordHash: string
    IsActive: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    RefreshTokens: RefreshToken list
}
