namespace HellArch.DataTypes.User

open System

/// Extended user profile information.
Linked to auth user via AuthUserId.
type UserProfile = {
    Id: Guid
    AuthUserId: Guid
    DisplayName: string option
    Bio: string option
    AvatarUrl: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
