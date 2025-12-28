namespace HellArch.DataTypes.Auth

open System

/// Core user identity in the auth system.
type User = {
    Id: Guid
    Email: string
    Username: string
    CreatedAt: DateTime
    UpdatedAt: DateTime
    Sessions: Session list
    LinkedAccounts: LinkedAccount list
}
