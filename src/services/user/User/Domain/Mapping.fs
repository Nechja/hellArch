namespace User.Domain

open System
open HellArch.DataTypes.User

module Mapping =

    module Theme =
        let toDto (theme: HellArch.DataTypes.User.Theme) : HellArch.User.V1.Theme =
            match theme with
            | Theme.Dark -> HellArch.User.V1.Theme.Dark
            | Theme.Light -> HellArch.User.V1.Theme.Light
            | Theme.System -> HellArch.User.V1.Theme.System

        let fromDto (theme: HellArch.User.V1.Theme) : HellArch.DataTypes.User.Theme =
            match theme with
            | HellArch.User.V1.Theme.Dark -> Theme.Dark
            | HellArch.User.V1.Theme.Light -> Theme.Light
            | HellArch.User.V1.Theme.System -> Theme.System

    module UserProfile =
        let toDto (entity: UserProfile) : HellArch.User.V1.UserProfile =
            { Id = entity.Id.ToString()
              Email = entity.Email
              DisplayName = entity.DisplayName |> Option.defaultValue ""
              AvatarUrl = entity.AvatarUrl |> Option.defaultValue ""
              Bio = entity.Bio |> Option.defaultValue ""
              CreatedAt = Some entity.CreatedAt
              UpdatedAt = Some entity.UpdatedAt }

    module NotificationSettings =
        let toDto (entity: UserPreferences) : HellArch.User.V1.NotificationSettings =
            { EmailEnabled = entity.EmailNotifications
              PushEnabled = entity.PushNotifications
              InAppEnabled = entity.InAppNotifications }

        let fromDto (dto: HellArch.User.V1.NotificationSettings option) =
            match dto with
            | Some n -> (n.EmailEnabled, n.PushEnabled, n.InAppEnabled)
            | None -> (true, true, true)

    module UserPreferences =
        let toDto (entity: UserPreferences) : HellArch.User.V1.UserPreferences =
            { Theme = Theme.toDto entity.Theme
              Locale = entity.Locale |> Option.defaultValue "en-US"
              Timezone = entity.Timezone |> Option.defaultValue "UTC"
              Notifications = Some (NotificationSettings.toDto entity)
              Custom = None }

        let fromDto (userId: Guid) (dto: HellArch.User.V1.UserPreferences) : UserPreferences =
            let (email, push, inApp) = NotificationSettings.fromDto dto.Notifications
            { Id = Guid.NewGuid()
              UserProfileId = userId
              Theme = Theme.fromDto dto.Theme
              Locale = if String.IsNullOrEmpty dto.Locale then None else Some dto.Locale
              Timezone = if String.IsNullOrEmpty dto.Timezone then None else Some dto.Timezone
              EmailNotifications = email
              PushNotifications = push
              InAppNotifications = inApp
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow }
