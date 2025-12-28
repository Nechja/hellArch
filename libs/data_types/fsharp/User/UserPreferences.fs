namespace HellArch.DataTypes.User

open System

type Theme =
    | System
    | Light
    | Dark

/// User preferences and settings.
type UserPreferences = {
    Id: Guid
    UserProfileId: Guid
    Theme: Theme
    Locale: string option
    Timezone: string option
    EmailNotifications: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
    UserProfile: UserProfile option
}
