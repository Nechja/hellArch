namespace User.Domain

open System

[<RequireQualifiedAccess>]
type Theme =
    | Dark
    | Light
    | System

module Theme =
    let fromString =
        function
        | "dark" -> Theme.Dark
        | "light" -> Theme.Light
        | _ -> Theme.System

    let toString =
        function
        | Theme.Dark -> "dark"
        | Theme.Light -> "light"
        | Theme.System -> "system"

type NotificationSettings =
    { EmailEnabled: bool
      PushEnabled: bool
      InAppEnabled: bool }

type UserPreferences =
    { Theme: Theme
      Locale: string
      Timezone: string
      Notifications: NotificationSettings
      Custom: Map<string, string> }

module UserPreferences =
    let defaultPreferences =
        { Theme = Theme.Dark
          Locale = "en-US"
          Timezone = "UTC"
          Notifications =
            { EmailEnabled = true
              PushEnabled = true
              InAppEnabled = true }
          Custom = Map.empty }

type UserProfile =
    { Id: Guid
      Email: string
      DisplayName: string option
      AvatarUrl: string option
      Bio: string option
      CreatedAt: DateTimeOffset
      UpdatedAt: DateTimeOffset }

type LinkedAccount =
    { Provider: string
      ProviderUserId: string
      Email: string
      LinkedAt: DateTimeOffset }

type CreateProfileRequest =
    { UserId: Guid
      Email: string
      DisplayName: string option
      AvatarUrl: string option }

type UpdateProfileRequest =
    { DisplayName: string option
      AvatarUrl: string option
      Bio: string option }
