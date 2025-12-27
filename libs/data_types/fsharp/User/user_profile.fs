namespace HellArch.DataTypes.User

open System

type UserProfile = {
    Id: Guid
    AuthUserId: Guid
    DisplayName: string option
    Bio: string option
    AvatarUrl: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
