namespace User.Infrastructure

open System
open Npgsql.FSharp
open User.Domain

module Database =

    type DatabaseConfig = { ConnectionString: string }

    let private parseProfile (read: RowReader) : UserProfile =
        { Id = read.uuid "id"
          Email = read.string "email"
          DisplayName = read.stringOrNone "display_name"
          AvatarUrl = read.stringOrNone "avatar_url"
          Bio = read.stringOrNone "bio"
          CreatedAt = read.datetimeOffset "created_at"
          UpdatedAt = read.datetimeOffset "updated_at" }

    let private parsePreferences (read: RowReader) : UserPreferences =
        { Theme = read.string "theme" |> Theme.fromString
          Locale = read.string "locale"
          Timezone = read.string "timezone"
          Notifications =
            { EmailEnabled = read.bool "email_enabled"
              PushEnabled = read.bool "push_enabled"
              InAppEnabled = read.bool "in_app_enabled" }
          Custom = Map.empty }

    let getProfile (config: DatabaseConfig) (userId: Guid) =
        async {
            try
                let! result =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_profiles WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.uuid userId ]
                    |> Sql.executeRowAsync parseProfile
                    |> Async.AwaitTask

                return Ok result
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }

    let getProfileOption (config: DatabaseConfig) (userId: Guid) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_profiles WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.uuid userId ]
                    |> Sql.executeAsync parseProfile
                    |> Async.AwaitTask

                return Ok(results |> List.tryHead)
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }

    let createProfile (config: DatabaseConfig) (request: CreateProfileRequest) =
        async {
            try
                let! result =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query
                        """
                        INSERT INTO user_profiles (id, email, display_name, avatar_url)
                        VALUES (@id, @email, @display_name, @avatar_url)
                        RETURNING *
                        """
                    |> Sql.parameters
                        [ "id", Sql.uuid request.UserId
                          "email", Sql.string request.Email
                          "display_name", Sql.stringOrNone request.DisplayName
                          "avatar_url", Sql.stringOrNone request.AvatarUrl ]
                    |> Sql.executeRowAsync parseProfile
                    |> Async.AwaitTask

                return Ok result
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }

    let updateProfile (config: DatabaseConfig) (userId: Guid) (request: UpdateProfileRequest) =
        async {
            try
                let! result =
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
                        [ "id", Sql.uuid userId
                          "display_name", Sql.stringOrNone request.DisplayName
                          "avatar_url", Sql.stringOrNone request.AvatarUrl
                          "bio", Sql.stringOrNone request.Bio ]
                    |> Sql.executeRowAsync parseProfile
                    |> Async.AwaitTask

                return Ok result
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }

    let getPreferences (config: DatabaseConfig) (userId: Guid) =
        async {
            try
                let! results =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query "SELECT * FROM user_preferences WHERE user_id = @user_id"
                    |> Sql.parameters [ "user_id", Sql.uuid userId ]
                    |> Sql.executeAsync parsePreferences
                    |> Async.AwaitTask

                return
                    Ok(
                        results
                        |> List.tryHead
                        |> Option.defaultValue UserPreferences.defaultPreferences
                    )
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }

    let upsertPreferences (config: DatabaseConfig) (userId: Guid) (prefs: UserPreferences) =
        async {
            try
                let! _ =
                    config.ConnectionString
                    |> Sql.connect
                    |> Sql.query
                        """
                        INSERT INTO user_preferences (user_id, theme, locale, timezone, email_enabled, push_enabled, in_app_enabled)
                        VALUES (@user_id, @theme, @locale, @timezone, @email_enabled, @push_enabled, @in_app_enabled)
                        ON CONFLICT (user_id) DO UPDATE SET
                            theme = @theme,
                            locale = @locale,
                            timezone = @timezone,
                            email_enabled = @email_enabled,
                            push_enabled = @push_enabled,
                            in_app_enabled = @in_app_enabled,
                            updated_at = NOW()
                        """
                    |> Sql.parameters
                        [ "user_id", Sql.uuid userId
                          "theme", Sql.string (Theme.toString prefs.Theme)
                          "locale", Sql.string prefs.Locale
                          "timezone", Sql.string prefs.Timezone
                          "email_enabled", Sql.bool prefs.Notifications.EmailEnabled
                          "push_enabled", Sql.bool prefs.Notifications.PushEnabled
                          "in_app_enabled", Sql.bool prefs.Notifications.InAppEnabled ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask

                return Ok prefs
            with ex ->
                return Error(UserProfileOperations.DatabaseError ex.Message)
        }
