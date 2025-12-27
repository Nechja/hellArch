namespace HellArch.DataTypes.Auth

open System

type User = {
    Id: Guid
    Email: string
    Username: string
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
