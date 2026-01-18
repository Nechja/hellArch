namespace Identity.Infrastructure

open System
open System.Security.Cryptography
open System.Text

module TokenGenerator =

    /// Generate cryptographically secure random token
    let generateToken (length: int) =
        let bytes = RandomNumberGenerator.GetBytes(length)
        Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=')

    /// Generate authorization code (64 bytes)
    let generateAuthorizationCode () =
        generateToken 64

    /// Generate access token (48 bytes)
    let generateAccessToken () =
        generateToken 48

    /// Generate refresh token (64 bytes)
    let generateRefreshToken () =
        generateToken 64

    /// Generate client secret (32 bytes)
    let generateClientSecret () =
        generateToken 32

    /// Hash a token using SHA-256 for secure storage
    /// Tokens are looked up by their hash, not the raw value
    let hashToken (token: string) =
        use sha256 = SHA256.Create()
        let hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(token))
        Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=')

    /// Verify PKCE code challenge (S256 method only - PLAIN is not secure)
    /// Uses constant-time comparison to prevent timing attacks
    let verifyCodeChallenge (codeVerifier: string) (codeChallenge: string) (method: string) =
        match method.ToUpperInvariant() with
        | "S256" ->
            use sha256 = SHA256.Create()
            let hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier))
            let computed =
                Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=')
            // Use constant-time comparison to prevent timing attacks
            let computedBytes = Encoding.UTF8.GetBytes(computed)
            let challengeBytes = Encoding.UTF8.GetBytes(codeChallenge)
            CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>(computedBytes), ReadOnlySpan<byte>(challengeBytes))
        | _ -> false
