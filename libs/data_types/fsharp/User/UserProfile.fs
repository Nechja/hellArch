namespace HellArch.DataTypes.User

open System

/// Extended user profile information.
Linked to auth user via AuthUserId.
Email is denormalized from auth for read performance.
type UserProfile = {
    Id: Guid
    AuthUserId: Guid
    Email: string
    DisplayName: string option
    Bio: string option
    AvatarUrl: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
    Preferences: UserPreferences option
}
