namespace HellArch.DataTypes.User

open System

type Theme =
    | System
    | Light
    | Dark

/// Extended user profile information.
/// Linked to auth user via AuthUserId.
/// Email is denormalized from auth for read performance.
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

/// User preferences and settings.
and UserPreferences = {
    Id: Guid
    UserProfileId: Guid
    Theme: Theme
    Locale: string option
    Timezone: string option
    EmailNotifications: bool
    PushNotifications: bool
    InAppNotifications: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    UserProfile: UserProfile option
}

