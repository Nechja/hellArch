namespace User.Tests

open System
open Xunit
open User.Domain

/// Tests for validation logic and error handling.
/// These verify the service properly validates inputs and returns appropriate errors.
module ValidationTests =

    /// Tests for UserError types and their usage.
    module ErrorTypeTests =

        [<Fact>]
        let ``UserError.NotFound contains the user ID`` () =
            let userId = Guid.NewGuid()
            let error = UserError.NotFound userId

            match error with
            | UserError.NotFound id -> Assert.Equal(userId, id)
            | _ -> Assert.Fail("Expected NotFound error")

        [<Fact>]
        let ``UserError.ValidationError contains the message`` () =
            let message = "Invalid email format"
            let error = UserError.ValidationError message

            match error with
            | UserError.ValidationError msg -> Assert.Equal(message, msg)
            | _ -> Assert.Fail("Expected ValidationError")

        [<Fact>]
        let ``UserError.DatabaseError contains the message`` () =
            let message = "Connection timeout"
            let error = UserError.DatabaseError message

            match error with
            | UserError.DatabaseError msg -> Assert.Equal(message, msg)
            | _ -> Assert.Fail("Expected DatabaseError")

        [<Fact>]
        let ``UserError.Conflict contains the message`` () =
            let message = "Profile already exists"
            let error = UserError.Conflict message

            match error with
            | UserError.Conflict msg -> Assert.Equal(message, msg)
            | _ -> Assert.Fail("Expected Conflict error")

    /// Tests for UserResult type alias behavior.
    module ResultTests =

        [<Fact>]
        let ``UserResult Ok can hold a profile`` () =
            let profile: HellArch.DataTypes.User.UserProfile =
                {
                    Id = Guid.NewGuid()
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = Some "Test"
                    AvatarUrl = None
                    Bio = None
                    CreatedAt = DateTime.UtcNow
                    UpdatedAt = DateTime.UtcNow
                    Preferences = None
                }

            let result: UserResult<HellArch.DataTypes.User.UserProfile> = Ok profile

            match result with
            | Ok p -> Assert.Equal(profile.Id, p.Id)
            | Error _ -> Assert.Fail("Expected Ok result")

        [<Fact>]
        let ``UserResult Error can hold any UserError type`` () =
            let errors = [
                UserError.NotFound (Guid.NewGuid())
                UserError.ValidationError "test"
                UserError.DatabaseError "test"
                UserError.Conflict "test"
            ]

            for error in errors do
                let result: UserResult<string> = Error error
                match result with
                | Ok _ -> Assert.Fail("Expected Error result")
                | Error e -> Assert.Equal(error, e)

    /// Tests for CreateProfileFromEvent validation.
    module CreateProfileValidationTests =

        [<Fact>]
        let ``CreateProfileFromEvent requires valid AuthUserId`` () =
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.Empty
                    Email = "test@example.com"
                    DisplayName = Some "Test"
                    AvatarUrl = None
                }

            // Empty GUID is technically valid but represents uninitialized state
            Assert.Equal(Guid.Empty, request.AuthUserId)

        [<Fact>]
        let ``CreateProfileFromEvent allows empty DisplayName`` () =
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = None
                    AvatarUrl = None
                }

            Assert.True(request.DisplayName.IsNone)

        [<Fact>]
        let ``CreateProfileFromEvent allows empty AvatarUrl`` () =
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = Some "Test"
                    AvatarUrl = None
                }

            Assert.True(request.AvatarUrl.IsNone)

        [<Fact>]
        let ``CreateProfileFromEvent preserves whitespace in DisplayName`` () =
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = Some "  Spaced Name  "
                    AvatarUrl = None
                }

            Assert.Equal(Some "  Spaced Name  ", request.DisplayName)

    /// Tests for edge cases in data handling.
    module EdgeCaseTests =

        [<Fact>]
        let ``Empty email string is stored as-is`` () =
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = ""
                    DisplayName = None
                    AvatarUrl = None
                }

            Assert.Equal("", request.Email)

        [<Fact>]
        let ``Very long display name is accepted`` () =
            let longName = String.replicate 1000 "a"
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = Some longName
                    AvatarUrl = None
                }

            Assert.Equal(1000, request.DisplayName.Value.Length)

        [<Fact>]
        let ``Unicode characters in display name are preserved`` () =
            let unicodeName = "Test \u4e2d\u6587 \u0420\u0443\u0441\u0441\u043a\u0438\u0439 \ud83d\ude00"
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = Some unicodeName
                    AvatarUrl = None
                }

            Assert.Equal(unicodeName, request.DisplayName.Value)

        [<Fact>]
        let ``Special characters in bio are preserved`` () =
            let specialBio = "Line1\nLine2\tTabbed\r\nWindows line"
            let profile: HellArch.DataTypes.User.UserProfile =
                {
                    Id = Guid.NewGuid()
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = None
                    AvatarUrl = None
                    Bio = Some specialBio
                    CreatedAt = DateTime.UtcNow
                    UpdatedAt = DateTime.UtcNow
                    Preferences = None
                }

            Assert.Equal(specialBio, profile.Bio.Value)

        [<Fact>]
        let ``URL with query parameters in AvatarUrl is preserved`` () =
            let urlWithParams = "https://example.com/avatar.png?size=200&format=webp"
            let request: CreateProfileFromEvent =
                {
                    AuthUserId = Guid.NewGuid()
                    Email = "test@example.com"
                    DisplayName = None
                    AvatarUrl = Some urlWithParams
                }

            Assert.Equal(urlWithParams, request.AvatarUrl.Value)
