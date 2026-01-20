namespace User.Tests

open System
open Xunit
open User.Domain
open HellArch.DataTypes.User

/// Tests for domain mapping functions.
/// These verify correct conversion between domain entities and DTOs.
module MappingTests =

    /// Tests for Theme mapping between domain and DTO types.
    module ThemeTests =

        [<Theory>]
        [<InlineData("dark")>]
        [<InlineData("Dark")>]
        [<InlineData("DARK")>]
        let ``Theme.toDto converts Dark theme correctly`` (_: string) =
            let result = Mapping.Theme.toDto Theme.Dark
            Assert.Equal(HellArch.User.V1.Theme.Dark, result)

        [<Theory>]
        [<InlineData("light")>]
        [<InlineData("Light")>]
        [<InlineData("LIGHT")>]
        let ``Theme.toDto converts Light theme correctly`` (_: string) =
            let result = Mapping.Theme.toDto Theme.Light
            Assert.Equal(HellArch.User.V1.Theme.Light, result)

        [<Fact>]
        let ``Theme.toDto converts System theme correctly`` () =
            let result = Mapping.Theme.toDto Theme.System
            Assert.Equal(HellArch.User.V1.Theme.System, result)

        [<Fact>]
        let ``Theme.fromDto roundtrips Dark correctly`` () =
            let original = Theme.Dark
            let dto = Mapping.Theme.toDto original
            let result = Mapping.Theme.fromDto dto
            Assert.Equal(original, result)

        [<Fact>]
        let ``Theme.fromDto roundtrips Light correctly`` () =
            let original = Theme.Light
            let dto = Mapping.Theme.toDto original
            let result = Mapping.Theme.fromDto dto
            Assert.Equal(original, result)

        [<Fact>]
        let ``Theme.fromDto roundtrips System correctly`` () =
            let original = Theme.System
            let dto = Mapping.Theme.toDto original
            let result = Mapping.Theme.fromDto dto
            Assert.Equal(original, result)

    /// Tests for UserProfile mapping.
    module UserProfileTests =

        let createTestProfile () : UserProfile =
            {
                Id = Guid.NewGuid()
                AuthUserId = Guid.NewGuid()
                Email = "test@example.com"
                DisplayName = Some "Test User"
                AvatarUrl = Some "https://example.com/avatar.png"
                Bio = Some "A test bio"
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
                Preferences = None
            }

        [<Fact>]
        let ``UserProfile.toDto maps all fields correctly`` () =
            let profile = createTestProfile()
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal(profile.Id.ToString(), dto.Id)
            Assert.Equal(profile.AuthUserId.ToString(), dto.AuthUserId)
            Assert.Equal(profile.Email, dto.Email)
            Assert.Equal("Test User", dto.DisplayName)
            Assert.Equal("https://example.com/avatar.png", dto.AvatarUrl)
            Assert.Equal("A test bio", dto.Bio)

        [<Fact>]
        let ``UserProfile.toDto handles None DisplayName as empty string`` () =
            let profile = { createTestProfile() with DisplayName = None }
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal("", dto.DisplayName)

        [<Fact>]
        let ``UserProfile.toDto handles None AvatarUrl as empty string`` () =
            let profile = { createTestProfile() with AvatarUrl = None }
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal("", dto.AvatarUrl)

        [<Fact>]
        let ``UserProfile.toDto handles None Bio as empty string`` () =
            let profile = { createTestProfile() with Bio = None }
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal("", dto.Bio)

        [<Fact>]
        let ``UserProfile.toDto handles all optional fields as None`` () =
            let profile =
                { createTestProfile() with
                    DisplayName = None
                    AvatarUrl = None
                    Bio = None
                }
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal("", dto.DisplayName)
            Assert.Equal("", dto.AvatarUrl)
            Assert.Equal("", dto.Bio)

        [<Fact>]
        let ``UserProfile.toDto preserves GUID format`` () =
            let id = Guid.Parse("12345678-1234-1234-1234-123456789012")
            let authId = Guid.Parse("87654321-4321-4321-4321-210987654321")
            let profile = { createTestProfile() with Id = id; AuthUserId = authId }
            let dto = Mapping.UserProfile.toDto profile

            Assert.Equal("12345678-1234-1234-1234-123456789012", dto.Id)
            Assert.Equal("87654321-4321-4321-4321-210987654321", dto.AuthUserId)

    /// Tests for UserPreferences mapping.
    module UserPreferencesTests =

        let createTestPreferences (userId: Guid) : UserPreferences =
            {
                Id = Guid.NewGuid()
                UserProfileId = userId
                Theme = Theme.Dark
                Locale = Some "en-US"
                Timezone = Some "America/New_York"
                EmailNotifications = true
                PushNotifications = false
                InAppNotifications = true
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

        [<Fact>]
        let ``UserPreferences.toDto maps theme correctly`` () =
            let prefs = createTestPreferences (Guid.NewGuid())
            let dto = Mapping.UserPreferences.toDto prefs

            Assert.Equal(HellArch.User.V1.Theme.Dark, dto.Theme)

        [<Fact>]
        let ``UserPreferences.toDto defaults locale to en-US when None`` () =
            let prefs = { createTestPreferences (Guid.NewGuid()) with Locale = None }
            let dto = Mapping.UserPreferences.toDto prefs

            Assert.Equal("en-US", dto.Locale)

        [<Fact>]
        let ``UserPreferences.toDto defaults timezone to UTC when None`` () =
            let prefs = { createTestPreferences (Guid.NewGuid()) with Timezone = None }
            let dto = Mapping.UserPreferences.toDto prefs

            Assert.Equal("UTC", dto.Timezone)

        [<Fact>]
        let ``UserPreferences.toDto maps notification settings correctly`` () =
            let prefs =
                { createTestPreferences (Guid.NewGuid()) with
                    EmailNotifications = true
                    PushNotifications = false
                    InAppNotifications = true
                }
            let dto = Mapping.UserPreferences.toDto prefs

            Assert.True(dto.Notifications.IsSome)
            let notifications = dto.Notifications.Value
            Assert.True(notifications.EmailEnabled)
            Assert.False(notifications.PushEnabled)
            Assert.True(notifications.InAppEnabled)

        [<Fact>]
        let ``UserPreferences.fromDto creates new ID`` () =
            let userId = Guid.NewGuid()
            let dto: HellArch.User.V1.UserPreferences =
                {
                    Theme = HellArch.User.V1.Theme.Light
                    Locale = "fr-FR"
                    Timezone = "Europe/Paris"
                    Notifications = Some { EmailEnabled = true; PushEnabled = true; InAppEnabled = true }
                    Custom = None
                }

            let prefs1 = Mapping.UserPreferences.fromDto userId dto
            let prefs2 = Mapping.UserPreferences.fromDto userId dto

            Assert.NotEqual(prefs1.Id, prefs2.Id)

        [<Fact>]
        let ``UserPreferences.fromDto preserves UserProfileId`` () =
            let userId = Guid.NewGuid()
            let dto: HellArch.User.V1.UserPreferences =
                {
                    Theme = HellArch.User.V1.Theme.System
                    Locale = "en-GB"
                    Timezone = "Europe/London"
                    Notifications = None
                    Custom = None
                }

            let prefs = Mapping.UserPreferences.fromDto userId dto

            Assert.Equal(userId, prefs.UserProfileId)

        [<Fact>]
        let ``UserPreferences.fromDto handles empty locale as None`` () =
            let userId = Guid.NewGuid()
            let dto: HellArch.User.V1.UserPreferences =
                {
                    Theme = HellArch.User.V1.Theme.Dark
                    Locale = ""
                    Timezone = "UTC"
                    Notifications = None
                    Custom = None
                }

            let prefs = Mapping.UserPreferences.fromDto userId dto

            Assert.True(prefs.Locale.IsNone)

        [<Fact>]
        let ``UserPreferences.fromDto handles empty timezone as None`` () =
            let userId = Guid.NewGuid()
            let dto: HellArch.User.V1.UserPreferences =
                {
                    Theme = HellArch.User.V1.Theme.Dark
                    Locale = "en-US"
                    Timezone = ""
                    Notifications = None
                    Custom = None
                }

            let prefs = Mapping.UserPreferences.fromDto userId dto

            Assert.True(prefs.Timezone.IsNone)

        [<Fact>]
        let ``UserPreferences.fromDto defaults notifications to all true when None`` () =
            let userId = Guid.NewGuid()
            let dto: HellArch.User.V1.UserPreferences =
                {
                    Theme = HellArch.User.V1.Theme.Dark
                    Locale = "en-US"
                    Timezone = "UTC"
                    Notifications = None
                    Custom = None
                }

            let prefs = Mapping.UserPreferences.fromDto userId dto

            Assert.True(prefs.EmailNotifications)
            Assert.True(prefs.PushNotifications)
            Assert.True(prefs.InAppNotifications)

    /// Tests for NotificationSettings mapping.
    module NotificationSettingsTests =

        [<Fact>]
        let ``NotificationSettings.fromDto handles Some correctly`` () =
            let dto: HellArch.User.V1.NotificationSettings =
                { EmailEnabled = false; PushEnabled = true; InAppEnabled = false }

            let (email, push, inApp) = Mapping.NotificationSettings.fromDto (Some dto)

            Assert.False(email)
            Assert.True(push)
            Assert.False(inApp)

        [<Fact>]
        let ``NotificationSettings.fromDto defaults to all true when None`` () =
            let (email, push, inApp) = Mapping.NotificationSettings.fromDto None

            Assert.True(email)
            Assert.True(push)
            Assert.True(inApp)
