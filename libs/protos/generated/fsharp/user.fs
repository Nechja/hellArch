namespace HellArch.User.V1

open System

type Theme =
    | Dark
    | Light
    | System

type GetProfileRequest = {
    UserId: string
}

and GetProfileResponse = {
    Profile: UserProfile option
}

and CreateProfileRequest = {
    UserId: string
    Email: string
    DisplayName: string
    AvatarUrl: string
}

and CreateProfileResponse = {
    Profile: UserProfile option
}

and UpdateProfileRequest = {
    UserId: string
    DisplayName: string option
    AvatarUrl: string option
    Bio: string option
}

and UpdateProfileResponse = {
    Profile: UserProfile option
}

and UserProfile = {
    Id: string
    AuthUserId: string
    Email: string
    DisplayName: string
    AvatarUrl: string
    Bio: string
    CreatedAt: DateTime option
    UpdatedAt: DateTime option
}

and GetPreferencesRequest = {
    UserId: string
}

and GetPreferencesResponse = {
    Preferences: UserPreferences option
}

and UpdatePreferencesRequest = {
    UserId: string
    Preferences: UserPreferences option
}

and UpdatePreferencesResponse = {
    Preferences: UserPreferences option
}

and UserPreferences = {
    Theme: Theme
    Locale: string
    Timezone: string
    Notifications: NotificationSettings option
    Custom: Map<string, obj> option
}

and NotificationSettings = {
    EmailEnabled: bool
    PushEnabled: bool
    InAppEnabled: bool
}

and GetLinkedAccountsRequest = {
    UserId: string
}

and GetLinkedAccountsResponse = {
    Accounts: LinkedAccount list
}

and LinkAccountRequest = {
    UserId: string
    Provider: string
    ProviderUserId: string
    Email: string
}

and LinkAccountResponse = {
    Account: LinkedAccount option
}

and UnlinkAccountRequest = {
    UserId: string
    Provider: string
}

and UnlinkAccountResponse = {
    Success: bool
}

and LinkedAccount = {
    Provider: string
    ProviderUserId: string
    Email: string
    LinkedAt: DateTime option
}

