namespace HellArch.DataTypes.Auth

open System

type Session = {
    Id: Guid
    UserId: Guid
    Token: string
    ExpiresAt: DateTime
    CreatedAt: DateTime
}
