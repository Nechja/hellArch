namespace Identity.Api

open System

module Views =

    /// Build login page URL with authorization session
    let loginUrl (baseUrl: string) (sessionId: string) =
        $"{baseUrl}/login?session={sessionId}"

    /// Build register page URL with authorization session
    let registerUrl (baseUrl: string) (sessionId: string) =
        $"{baseUrl}/register?session={sessionId}"

    /// Build callback URL with authorization code
    let callbackUrl (redirectUri: string) (code: string) (state: string) =
        let separator = if redirectUri.Contains("?") then "&" else "?"
        $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}"

    /// Build error callback URL
    let errorCallbackUrl (redirectUri: string) (error: string) (state: string) =
        let separator = if redirectUri.Contains("?") then "&" else "?"
        $"{redirectUri}{separator}error={Uri.EscapeDataString(error)}&state={Uri.EscapeDataString(state)}"
