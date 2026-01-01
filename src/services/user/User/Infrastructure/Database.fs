namespace User.Infrastructure

open System
open Npgsql.FSharp
open HellArch.DataTypes.User
open User.Domain

module Database =

    type DatabaseConfig = { ConnectionString: string }

    let checkConnection (config: DatabaseConfig) =
        async {
            try
                let! _ =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT 1"
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                return true
            with _ ->
                return false
        }

    let private parseTheme (value: string) : Theme =
        match value.ToLowerInvariant() with
        | "dark" -> Theme.Dark
        | "light" -> Theme.Light
        | _ -> Theme.System

    let private themeToString (theme: Theme) : string =
        match theme with
        | Theme.Dark -> "dark"
        | Theme.Light -> "light"
        | Theme.System -> "system"

    let private parseProfile (read: RowReader) : UserProfile =
        {
            Id = read.uuid "id"
            AuthUserId = read.uuid "auth_user_id"
            Email = read.string "email"
            DisplayName = read.stringOrNone "display_name"
            AvatarUrl = read.stringOrNone "avatar_url"
            Bio = read.stringOrNone "bio"
            CreatedAt = read.dateTime "created_at"
            UpdatedAt = read.dateTime "updated_at"
            Preferences = None
        }

    let private parsePreferences (read: RowReader) : UserPreferences =
        {
            Id = read.uuid "id"
            UserProfileId = read.uuid "user_profile_id"
            Theme = read.string "theme" |> parseTheme
            Locale = read.stringOrNone "locale"
            Timezone = read.stringOrNone "timezone"
            EmailNotifications = read.bool "email_notifications"
            PushNotifications = read.bool "push_notifications"
            InAppNotifications = read.bool "in_app_notifications"
            CreatedAt = read.dateTime "created_at"
            UpdatedAt = read.dateTime "updated_at"
        }

    let getProfile (config: DatabaseConfig) (userId: Guid) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_profiles WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.uuid userId ]
                    |> Sql.executeAsync parseProfile
                    |> Async.AwaitTask

                match results with
                | [ profile ] -> return Ok profile
                | [] -> return Error(UserError.NotFound userId)
                | _ -> return Error(UserError.DatabaseError "Multiple profiles found for same ID")
            with ex ->
                return Error(UserError.DatabaseError ex.Message)
        }

    let getProfileByAuthUserId (config: DatabaseConfig) (authUserId: Guid) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_profiles WHERE auth_user_id = @auth_user_id"
                    |> Sql.parameters [ "auth_user_id", Sql.uuid authUserId ]
                    |> Sql.executeAsync parseProfile
                    |> Async.AwaitTask

                return Ok(results |> List.tryHead)
            with ex ->
                return Error(UserError.DatabaseError ex.Message)
        }

    let createProfile (config: DatabaseConfig) (request: CreateProfileFromEvent) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query
                        """
                        INSERT INTO user_profiles (id, auth_user_id, email, display_name, avatar_url)
                        VALUES (@id, @auth_user_id, @email, @display_name, @avatar_url)
                        RETURNING *
                        """
                    |> Sql.parameters
                        [
                            "id", Sql.uuid (Guid.NewGuid())
                            "auth_user_id", Sql.uuid request.AuthUserId
                            "email", Sql.string request.Email
                            "display_name", Sql.stringOrNone request.DisplayName
                            "avatar_url", Sql.stringOrNone request.AvatarUrl
                        ]
                    |> Sql.executeAsync parseProfile
                    |> Async.AwaitTask

                match results with
                | [ profile ] -> return Ok profile
                | _ -> return Error(UserError.DatabaseError "Insert did not return expected row")
            with
            | :? Npgsql.PostgresException as ex when ex.SqlState = "23505" ->
                return Error(UserError.Conflict "Profile already exists for this user")
            | ex ->
                return Error(UserError.DatabaseError ex.Message)
        }

    let updateProfile
        (config: DatabaseConfig)
        (userId: Guid)
        (displayName: string option)
        (avatarUrl: string option)
        (bio: string option)
        =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query
                        """
                        UPDATE user_profiles
                        SET display_name = COALESCE(@display_name, display_name),
                            avatar_url = COALESCE(@avatar_url, avatar_url),
                            bio = COALESCE(@bio, bio),
                            updated_at = NOW()
                        WHERE id = @id
                        RETURNING *
                        """
                    |> Sql.parameters
                        [
                            "id", Sql.uuid userId
                            "display_name", Sql.stringOrNone displayName
                            "avatar_url", Sql.stringOrNone avatarUrl
                            "bio", Sql.stringOrNone bio
                        ]
                    |> Sql.executeAsync parseProfile
                    |> Async.AwaitTask

                match results with
                | [ profile ] -> return Ok profile
                | [] -> return Error(UserError.NotFound userId)
                | _ -> return Error(UserError.DatabaseError "Update returned multiple rows")
            with ex ->
                return Error(UserError.DatabaseError ex.Message)
        }

    let getPreferences (config: DatabaseConfig) (userId: Guid) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_preferences WHERE user_profile_id = @user_profile_id"
                    |> Sql.parameters [ "user_profile_id", Sql.uuid userId ]
                    |> Sql.executeAsync parsePreferences
                    |> Async.AwaitTask

                return Ok(results |> List.tryHead)
            with ex ->
                return Error(UserError.DatabaseError ex.Message)
        }

    let upsertPreferences (config: DatabaseConfig) (prefs: UserPreferences) =
        async {
            try
                let! _ =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query
                        """
                        INSERT INTO user_preferences (id, user_profile_id, theme, locale, timezone, email_notifications, push_notifications, in_app_notifications)
                        VALUES (@id, @user_profile_id, @theme, @locale, @timezone, @email_notifications, @push_notifications, @in_app_notifications)
                        ON CONFLICT (user_profile_id) DO UPDATE SET
                            theme = @theme,
                            locale = @locale,
                            timezone = @timezone,
                            email_notifications = @email_notifications,
                            push_notifications = @push_notifications,
                            in_app_notifications = @in_app_notifications,
                            updated_at = NOW()
                        """
                    |> Sql.parameters
                        [
                            "id", Sql.uuid prefs.Id
                            "user_profile_id", Sql.uuid prefs.UserProfileId
                            "theme", Sql.string (themeToString prefs.Theme)
                            "locale", Sql.stringOrNone prefs.Locale
                            "timezone", Sql.stringOrNone prefs.Timezone
                            "email_notifications", Sql.bool prefs.EmailNotifications
                            "push_notifications", Sql.bool prefs.PushNotifications
                            "in_app_notifications", Sql.bool prefs.InAppNotifications
                        ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask

                return Ok prefs
            with ex ->
                return Error(UserError.DatabaseError ex.Message)
        }
