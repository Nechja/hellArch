namespace HellArch.DataTypes.User

open System

type Theme =
    | System
    | Light
    | Dark

type UserPreferences = {
    Id: Guid
    UserProfileId: Guid
    Theme: Theme
    Locale: string option
    Timezone: string option
    EmailNotifications: bool
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
