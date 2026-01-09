namespace Identity.Infrastructure

open System
open System.Net.Http
open System.Security.Cryptography
open System.Text

module BreachDetection =

    let private httpClient = new HttpClient()

    /// SHA-1 hash a string and return uppercase hex
    let private sha1Hash (input: string) =
        use sha1 = SHA1.Create()
        let bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input))
        BitConverter.ToString(bytes).Replace("-", "")

    /// Check if a password has been exposed in a data breach using HaveIBeenPwned API.
    /// Uses k-Anonymity: only sends first 5 chars of SHA-1 hash.
    /// Returns: Ok (Some count) if breached, Ok None if not breached, Error on API failure
    let checkPasswordBreach (password: string) =
        async {
            try
                let hash = sha1Hash password
                let prefix = hash.Substring(0, 5)
                let suffix = hash.Substring(5)

                let url = $"https://api.pwnedpasswords.com/range/{prefix}"
                httpClient.DefaultRequestHeaders.Clear()
                httpClient.DefaultRequestHeaders.Add("User-Agent", "HellArch-Identity-Service")

                let! response = httpClient.GetAsync(url) |> Async.AwaitTask

                if response.IsSuccessStatusCode then
                    let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                    // Response format: SUFFIX:COUNT per line
                    let breachCount =
                        content.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
                        |> Array.tryPick (fun line ->
                            let parts = line.Split(':')
                            if parts.Length = 2 && String.Equals(parts.[0], suffix, StringComparison.OrdinalIgnoreCase) then
                                Some (Int32.Parse(parts.[1]))
                            else
                                None)

                    return Ok breachCount
                else
                    // API error - don't block registration, just log
                    return Error $"HIBP API returned {response.StatusCode}"
            with ex ->
                // Network error - don't block registration, just log
                return Error $"HIBP check failed: {ex.Message}"
        }

    /// Check password and return user-friendly error if breached
    let validateNotBreached (password: string) =
        async {
            let! result = checkPasswordBreach password
            match result with
            | Ok (Some count) when count > 0 ->
                return Error $"This password has been exposed in data breaches ({count:N0} times). Please choose a different password."
            | Ok _ ->
                return Ok ()
            | Error msg ->
                // Log the error but don't block registration
                printfn "HIBP check failed (non-blocking): %s" msg
                return Ok ()
        }
