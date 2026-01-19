namespace Identity.Tests

open System
open Xunit
open Identity.Infrastructure

/// Tests that verify security properties of the Identity service.
/// These tests prevent regressions that could introduce security vulnerabilities.
module SecurityTests =

    /// Tests for PKCE code challenge verification.
    /// These ensure the implementation correctly validates code verifiers.
    module PkceTests =

        [<Fact>]
        let ``verifyCodeChallenge accepts valid S256 challenge`` () =
            // Known test vector: "test_verifier" hashed with SHA256, base64url encoded
            // SHA256("test_verifier") = base64url encoded result
            let verifier = "test_verifier_that_is_long_enough_for_pkce"

            // Generate the expected challenge from the verifier
            use sha256 = System.Security.Cryptography.SHA256.Create()
            let hash = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(verifier))
            let expectedChallenge =
                Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=')

            let result = TokenGenerator.verifyCodeChallenge verifier expectedChallenge "S256"

            Assert.True(result, "Valid S256 challenge should be accepted")

        [<Fact>]
        let ``verifyCodeChallenge rejects wrong verifier`` () =
            let verifier = "correct_verifier"
            let wrongVerifier = "wrong_verifier"

            // Generate challenge from correct verifier
            use sha256 = System.Security.Cryptography.SHA256.Create()
            let hash = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(verifier))
            let challenge =
                Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=')

            let result = TokenGenerator.verifyCodeChallenge wrongVerifier challenge "S256"

            Assert.False(result, "Wrong verifier should be rejected")

        [<Fact>]
        let ``verifyCodeChallenge rejects PLAIN method`` () =
            // PLAIN method is insecure and should be rejected
            let verifier = "test_verifier"
            let challenge = verifier  // PLAIN just uses the verifier directly

            let result = TokenGenerator.verifyCodeChallenge verifier challenge "plain"

            Assert.False(result, "PLAIN method should be rejected as insecure")

        [<Fact>]
        let ``verifyCodeChallenge rejects unknown methods`` () =
            let verifier = "test_verifier"
            let challenge = "some_challenge"

            let result = TokenGenerator.verifyCodeChallenge verifier challenge "MD5"

            Assert.False(result, "Unknown challenge method should be rejected")

        [<Theory>]
        [<InlineData("s256")>]
        [<InlineData("S256")>]
        [<InlineData("S256")>]
        let ``verifyCodeChallenge accepts S256 case-insensitively`` (method: string) =
            let verifier = "test_verifier"

            use sha256 = System.Security.Cryptography.SHA256.Create()
            let hash = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(verifier))
            let challenge =
                Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=')

            let result = TokenGenerator.verifyCodeChallenge verifier challenge method

            Assert.True(result, $"Method '{method}' should be accepted case-insensitively")

    /// Tests for token generation security properties.
    module TokenGenerationTests =

        [<Fact>]
        let ``generateToken produces cryptographically random output`` () =
            // Generate multiple tokens and verify they're all unique
            let tokens =
                [1..100]
                |> List.map (fun _ -> TokenGenerator.generateToken 32)
                |> Set.ofList

            // All 100 tokens should be unique
            Assert.Equal(100, tokens.Count)

        [<Fact>]
        let ``generateToken produces URL-safe output`` () =
            // URL-unsafe characters (+, /, =) should be replaced
            let tokens =
                [1..50]
                |> List.map (fun _ -> TokenGenerator.generateToken 64)

            for token in tokens do
                Assert.DoesNotContain("+", token)
                Assert.DoesNotContain("/", token)
                Assert.DoesNotContain("=", token)

        [<Fact>]
        let ``hashToken produces consistent output`` () =
            let token = "test_token_value"

            let hash1 = TokenGenerator.hashToken token
            let hash2 = TokenGenerator.hashToken token

            Assert.Equal(hash1, hash2)

        [<Fact>]
        let ``hashToken produces different output for different inputs`` () =
            let hash1 = TokenGenerator.hashToken "token1"
            let hash2 = TokenGenerator.hashToken "token2"

            Assert.NotEqual<string>(hash1, hash2)

    /// Tests for password hashing security properties.
    module PasswordHashingTests =

        [<Fact>]
        let ``hash produces verifiable output`` () =
            let password = "SecurePassword123!"
            let hash = PasswordHasher.hash password

            let result = PasswordHasher.verify password hash

            Assert.True(result, "Hash should be verifiable with correct password")

        [<Fact>]
        let ``verify rejects wrong password`` () =
            let password = "SecurePassword123!"
            let wrongPassword = "WrongPassword456!"
            let hash = PasswordHasher.hash password

            let result = PasswordHasher.verify wrongPassword hash

            Assert.False(result, "Wrong password should be rejected")

        [<Fact>]
        let ``hash produces unique output for same password`` () =
            // Each hash should include a unique salt
            let password = "SecurePassword123!"

            let hash1 = PasswordHasher.hash password
            let hash2 = PasswordHasher.hash password

            Assert.NotEqual<string>(hash1, hash2)

        [<Fact>]
        let ``verify handles empty password`` () =
            let hash = PasswordHasher.hash "ValidPassword123!"

            // Should not throw, just return false
            let result = PasswordHasher.verify "" hash

            Assert.False(result)
