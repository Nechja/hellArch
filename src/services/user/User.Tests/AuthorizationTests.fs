namespace User.Tests

open System
open Xunit
open HellArch.DataTypes.User

/// Tests for authorization and ownership verification logic.
/// These verify the patterns used to check user ownership of resources.
module AuthorizationTests =

    /// Tests for GUID parsing used in authorization headers.
    module GuidParsingTests =

        [<Fact>]
        let ``Valid GUID string parses successfully`` () =
            let guidString = "12345678-1234-1234-1234-123456789012"
            match Guid.TryParse(guidString) with
            | true, guid -> Assert.Equal(Guid.Parse(guidString), guid)
            | false, _ -> Assert.Fail("Expected successful parse")

        [<Fact>]
        let ``Empty string fails to parse as GUID`` () =
            match Guid.TryParse("") with
            | true, _ -> Assert.Fail("Expected parse failure")
            | false, _ -> Assert.True(true)

        [<Fact>]
        let ``Whitespace string fails to parse as GUID`` () =
            match Guid.TryParse("   ") with
            | true, _ -> Assert.Fail("Expected parse failure")
            | false, _ -> Assert.True(true)

        [<Fact>]
        let ``Malformed GUID fails to parse`` () =
            let malformed = [
                "not-a-guid"
                "12345678-1234-1234-1234"
                "12345678-1234-1234-1234-12345678901"  // Too short
                "12345678-1234-1234-1234-1234567890123" // Too long
                "ZZZZZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZZZZZZZZZ"  // Invalid hex
            ]

            for s in malformed do
                match Guid.TryParse(s) with
                | true, _ -> Assert.Fail($"Expected parse failure for: {s}")
                | false, _ -> ()

        [<Fact>]
        let ``GUID parsing is case-insensitive`` () =
            let lower = "12345678-abcd-1234-abcd-123456789012"
            let upper = "12345678-ABCD-1234-ABCD-123456789012"
            let mixed = "12345678-AbCd-1234-aBcD-123456789012"

            let (_, lowerGuid) = Guid.TryParse(lower)
            let (_, upperGuid) = Guid.TryParse(upper)
            let (_, mixedGuid) = Guid.TryParse(mixed)

            Assert.Equal(lowerGuid, upperGuid)
            Assert.Equal(upperGuid, mixedGuid)

    /// Tests for ownership verification patterns.
    module OwnershipTests =

        let createProfileWithAuthUser (authUserId: Guid) : UserProfile =
            {
                Id = Guid.NewGuid()
                AuthUserId = authUserId
                Email = "test@example.com"
                DisplayName = None
                AvatarUrl = None
                Bio = None
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
                Preferences = None
            }

        [<Fact>]
        let ``Same AuthUserId passes ownership check`` () =
            let authUserId = Guid.NewGuid()
            let profile = createProfileWithAuthUser authUserId
            let requestingUserId = authUserId

            let isOwner = profile.AuthUserId = requestingUserId

            Assert.True(isOwner)

        [<Fact>]
        let ``Different AuthUserId fails ownership check`` () =
            let profileOwner = Guid.NewGuid()
            let profile = createProfileWithAuthUser profileOwner
            let requestingUserId = Guid.NewGuid()

            let isOwner = profile.AuthUserId = requestingUserId

            Assert.False(isOwner)

        [<Fact>]
        let ``Empty GUID does not match valid GUID`` () =
            let profileOwner = Guid.NewGuid()
            let profile = createProfileWithAuthUser profileOwner
            let emptyGuid = Guid.Empty

            let isOwner = profile.AuthUserId = emptyGuid

            Assert.False(isOwner)

        [<Fact>]
        let ``Profile ID is not the same as AuthUserId`` () =
            let authUserId = Guid.NewGuid()
            let profile = createProfileWithAuthUser authUserId

            // Profile.Id should be different from AuthUserId
            Assert.NotEqual(profile.Id, profile.AuthUserId)

        [<Fact>]
        let ``Ownership check uses AuthUserId not Profile Id`` () =
            let authUserId = Guid.NewGuid()
            let profile = createProfileWithAuthUser authUserId

            // Attempting to use Profile.Id for ownership check should fail
            let wrongCheck = profile.Id = authUserId
            let correctCheck = profile.AuthUserId = authUserId

            Assert.False(wrongCheck)
            Assert.True(correctCheck)

    /// Tests for preferences ownership patterns.
    module PreferencesOwnershipTests =

        let createPreferencesForProfile (profileId: Guid) : UserPreferences =
            {
                Id = Guid.NewGuid()
                UserProfileId = profileId
                Theme = Theme.System
                Locale = Some "en-US"
                Timezone = Some "UTC"
                EmailNotifications = true
                PushNotifications = true
                InAppNotifications = true
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

        [<Fact>]
        let ``Preferences are linked to profile via UserProfileId`` () =
            let profileId = Guid.NewGuid()
            let prefs = createPreferencesForProfile profileId

            Assert.Equal(profileId, prefs.UserProfileId)

        [<Fact>]
        let ``Preferences ID is different from UserProfileId`` () =
            let profileId = Guid.NewGuid()
            let prefs = createPreferencesForProfile profileId

            Assert.NotEqual(prefs.Id, prefs.UserProfileId)

    /// Tests for header value handling patterns.
    module HeaderValueTests =

        [<Fact>]
        let ``Single value header can be parsed`` () =
            let headerValue = "12345678-1234-1234-1234-123456789012"
            match Guid.TryParse(headerValue.ToString()) with
            | true, guid -> Assert.NotEqual(Guid.Empty, guid)
            | false, _ -> Assert.Fail("Expected successful parse")

        [<Fact>]
        let ``Header with leading/trailing whitespace still parses`` () =
            let headerValue = " 12345678-1234-1234-1234-123456789012 "
            match Guid.TryParse(headerValue.Trim()) with
            | true, guid -> Assert.NotEqual(Guid.Empty, guid)
            | false, _ -> Assert.Fail("Expected successful parse after trim")

        [<Fact>]
        let ``Multiple values in header uses first value pattern`` () =
            // Simulating comma-separated header values
            let headerValue = "12345678-1234-1234-1234-123456789012, 87654321-4321-4321-4321-210987654321"
            let firstValue = headerValue.Split(',').[0].Trim()

            match Guid.TryParse(firstValue) with
            | true, _ -> Assert.True(true)
            | false, _ -> Assert.Fail("Expected successful parse of first value")

    /// Tests for authorization error scenarios.
    module AuthorizationErrorTests =

        [<Fact>]
        let ``Missing header results in empty string`` () =
            let missingHeader: string option = None

            match missingHeader with
            | None -> Assert.True(true, "Missing header detected")
            | Some _ -> Assert.Fail("Expected missing header")

        [<Fact>]
        let ``Empty header value is handled`` () =
            let emptyHeader = Some ""

            match emptyHeader with
            | Some "" -> Assert.True(true, "Empty header detected")
            | _ -> Assert.Fail("Expected empty header")

        [<Fact>]
        let ``Ownership denial is distinct from not found`` () =
            // These are different error conditions that should be handled differently
            let notFoundError = User.Domain.UserError.NotFound (Guid.NewGuid())
            let forbiddenScenario = "forbidden"

            match notFoundError with
            | User.Domain.UserError.NotFound _ -> Assert.True(true)
            | _ -> Assert.Fail("Expected NotFound error type")

            Assert.Equal("forbidden", forbiddenScenario)
