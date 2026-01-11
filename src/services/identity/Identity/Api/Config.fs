namespace Identity.Api

open System
open System.Collections.Concurrent
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http
open Identity.Domain

module Config =

    type IdentityConfig = {
        ConnectionString: string
        ClientBaseUrl: string
        AccessTokenTtl: TimeSpan
        RefreshTokenTtl: TimeSpan
        AuthCodeTtl: TimeSpan
    }

module SessionStore =

    let private authSessions = ConcurrentDictionary<string, AuthorizationSession>()

    let tryGet (sessionId: string) =
        match authSessions.TryGetValue(sessionId) with
        | true, session -> Some session
        | false, _ -> None

    let add (sessionId: string) (session: AuthorizationSession) =
        authSessions.[sessionId] <- session

    let tryRemove (sessionId: string) =
        authSessions.TryRemove(sessionId) |> ignore

module ClientFingerprint =

    let generate (ctx: HttpContext) =
        let ip = ctx.Connection.RemoteIpAddress |> Option.ofObj |> Option.map string |> Option.defaultValue "unknown"
        let ua = ctx.Request.Headers.["User-Agent"].ToString()
        let combined = $"{ip}|{ua}"
        use sha256 = SHA256.Create()
        let hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined))
        Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=')
        |> Some
