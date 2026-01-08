namespace Identity.Infrastructure

open System
open Npgsql.FSharp
open Identity.Domain

module Database =

    // User operations

    let getUserByEmail (connectionString: string) (email: string) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT id, email, username, password_hash, is_active, created_at, updated_at FROM identity_users WHERE email = @email"
                    |> Sql.parameters [ "email", Sql.string email ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            Id = read.uuid "id"
                            Email = read.string "email"
                            Username = read.string "username"
                            PasswordHash = read.string "password_hash"
                            IsActive = read.bool "is_active"
                            CreatedAt = read.dateTime "created_at"
                            UpdatedAt = read.dateTime "updated_at"
                        |})
                    |> Async.AwaitTask

                return
                    match result with
                    | [ user ] -> Ok (Some user)
                    | [] -> Ok None
                    | _ -> Error (IdentityError.DatabaseError "Multiple users found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let getUserById (connectionString: string) (userId: Guid) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT id, email, username, password_hash, is_active, created_at, updated_at FROM identity_users WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.uuid userId ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            Id = read.uuid "id"
                            Email = read.string "email"
                            Username = read.string "username"
                            PasswordHash = read.string "password_hash"
                            IsActive = read.bool "is_active"
                            CreatedAt = read.dateTime "created_at"
                            UpdatedAt = read.dateTime "updated_at"
                        |})
                    |> Async.AwaitTask

                return
                    match result with
                    | [ user ] -> Ok (Some user)
                    | [] -> Ok None
                    | _ -> Error (IdentityError.DatabaseError "Multiple users found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let private isUniqueViolation (ex: exn) =
        match ex with
        | :? Npgsql.PostgresException as pgEx -> pgEx.SqlState = "23505"
        | :? AggregateException as aggEx ->
            aggEx.InnerExceptions
            |> Seq.exists (fun inner ->
                match inner with
                | :? Npgsql.PostgresException as pgEx -> pgEx.SqlState = "23505"
                | _ -> false)
        | _ when ex.InnerException <> null ->
            match ex.InnerException with
            | :? Npgsql.PostgresException as pgEx -> pgEx.SqlState = "23505"
            | _ -> false
        | _ -> false

    let createUser (connectionString: string) (email: string) (username: string) (passwordHash: string) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO identity_users (email, username, password_hash) VALUES (@email, @username, @password_hash) RETURNING id"
                    |> Sql.parameters [
                        "email", Sql.string email
                        "username", Sql.string username
                        "password_hash", Sql.string passwordHash
                    ]
                    |> Sql.executeAsync (fun read -> read.uuid "id")
                    |> Async.AwaitTask

                return
                    match result with
                    | [ id ] -> Ok id
                    | _ -> Error (IdentityError.DatabaseError "Failed to create user")
            with
            | ex when isUniqueViolation ex ->
                return Error (IdentityError.UserExists email)
            | ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Client operations

    let getClient (connectionString: string) (clientId: string) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT id, secret_hash, name, redirect_uris, is_active FROM oauth_clients WHERE id = @id AND is_active = true"
                    |> Sql.parameters [ "id", Sql.string clientId ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            Id = read.string "id"
                            SecretHash = read.string "secret_hash"
                            Name = read.string "name"
                            RedirectUris = read.stringArray "redirect_uris" |> Array.toList
                            IsActive = read.bool "is_active"
                        |})
                    |> Async.AwaitTask

                return
                    match result with
                    | [ client ] -> Ok (Some client)
                    | [] -> Ok None
                    | _ -> Error (IdentityError.DatabaseError "Multiple clients found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let createClient (connectionString: string) (client: HellArch.DataTypes.Identity.OAuthClient) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO oauth_clients (id, secret_hash, name, redirect_uris, is_active, created_at) VALUES (@id, @secret_hash, @name, @redirect_uris, @is_active, @created_at)"
                    |> Sql.parameters [
                        "id", Sql.string client.Id
                        "secret_hash", Sql.string client.SecretHash
                        "name", Sql.string client.Name
                        "redirect_uris", Sql.stringArray (client.RedirectUris |> List.toArray)
                        "is_active", Sql.bool client.IsActive
                        "created_at", Sql.timestamptz client.CreatedAt
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Authorization code operations

    let createAuthorizationCode
        (connectionString: string)
        (code: string)
        (clientId: string)
        (userId: Guid)
        (redirectUri: string)
        (scope: string)
        (codeChallenge: string option)
        (codeChallengeMethod: string option)
        (expiresAt: DateTime)
        =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO authorization_codes (code, client_id, user_id, redirect_uri, scope, code_challenge, code_challenge_method, expires_at) VALUES (@code, @client_id, @user_id, @redirect_uri, @scope, @code_challenge, @code_challenge_method, @expires_at)"
                    |> Sql.parameters [
                        "code", Sql.string code
                        "client_id", Sql.string clientId
                        "user_id", Sql.uuid userId
                        "redirect_uri", Sql.string redirectUri
                        "scope", Sql.string scope
                        "code_challenge", Sql.stringOrNone codeChallenge
                        "code_challenge_method", Sql.stringOrNone codeChallengeMethod
                        "expires_at", Sql.timestamptz expiresAt
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let getAndDeleteAuthorizationCode (connectionString: string) (code: string) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM authorization_codes WHERE code = @code RETURNING client_id, user_id, redirect_uri, scope, code_challenge, code_challenge_method, expires_at"
                    |> Sql.parameters [ "code", Sql.string code ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            ClientId = read.string "client_id"
                            UserId = read.uuid "user_id"
                            RedirectUri = read.string "redirect_uri"
                            Scope = read.string "scope"
                            CodeChallenge = read.stringOrNone "code_challenge"
                            CodeChallengeMethod = read.stringOrNone "code_challenge_method"
                            ExpiresAt = read.dateTime "expires_at"
                        |})
                    |> Async.AwaitTask

                return
                    match result with
                    | [ authCode ] -> Ok (Some authCode)
                    | [] -> Ok None
                    | _ -> Error (IdentityError.DatabaseError "Multiple codes found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Access token operations

    let createAccessToken (connectionString: string) (token: string) (userId: Guid) (clientId: string) (scope: string) (expiresAt: DateTime) (fingerprint: string option) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO access_tokens (token_hash, user_id, client_id, scope, expires_at, client_fingerprint) VALUES (@token_hash, @user_id, @client_id, @scope, @expires_at, @fingerprint)"
                    |> Sql.parameters [
                        "token_hash", Sql.string tokenHash
                        "user_id", Sql.uuid userId
                        "client_id", Sql.string clientId
                        "scope", Sql.string scope
                        "expires_at", Sql.timestamptz expiresAt
                        "fingerprint", Sql.stringOrNone fingerprint
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let getAccessToken (connectionString: string) (token: string) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT user_id, client_id, scope, expires_at, client_fingerprint FROM access_tokens WHERE token_hash = @token_hash"
                    |> Sql.parameters [ "token_hash", Sql.string tokenHash ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            UserId = read.uuid "user_id"
                            ClientId = read.string "client_id"
                            Scope = read.string "scope"
                            ExpiresAt = read.dateTime "expires_at"
                            ClientFingerprint = read.stringOrNone "client_fingerprint"
                        |})
                    |> Async.AwaitTask

                return
                    match result with
                    | [ t ] -> Ok (Some t)
                    | [] -> Ok None
                    | _ -> Error (IdentityError.DatabaseError "Multiple tokens found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Refresh token operations

    /// Create a new refresh token with family tracking
    let createRefreshToken (connectionString: string) (token: string) (userId: Guid) (clientId: string) (scope: string) (expiresAt: DateTime) (familyId: Guid) (fingerprint: string option) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO refresh_tokens (token_hash, user_id, client_id, scope, expires_at, family_id, is_revoked, client_fingerprint) VALUES (@token_hash, @user_id, @client_id, @scope, @expires_at, @family_id, false, @fingerprint)"
                    |> Sql.parameters [
                        "token_hash", Sql.string tokenHash
                        "user_id", Sql.uuid userId
                        "client_id", Sql.string clientId
                        "scope", Sql.string scope
                        "expires_at", Sql.timestamptz expiresAt
                        "family_id", Sql.uuid familyId
                        "fingerprint", Sql.stringOrNone fingerprint
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    /// Revoke all tokens in a family (used when reuse is detected)
    let revokeTokenFamily (connectionString: string) (familyId: Guid) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "UPDATE refresh_tokens SET is_revoked = true WHERE family_id = @family_id"
                    |> Sql.parameters [ "family_id", Sql.uuid familyId ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    /// Get refresh token and mark as revoked (for rotation)
    /// Returns None if token not found, Error if already revoked (reuse detected)
    type RefreshTokenResult =
        | Valid of {| UserId: Guid; ClientId: string; Scope: string; ExpiresAt: DateTime; FamilyId: Guid; ClientFingerprint: string option |}
        | ReuseDetected of Guid // Returns FamilyId for revocation
        | NotFound

    let useRefreshToken (connectionString: string) (token: string) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                // First, get the token info
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT user_id, client_id, scope, expires_at, family_id, is_revoked, client_fingerprint FROM refresh_tokens WHERE token_hash = @token_hash"
                    |> Sql.parameters [ "token_hash", Sql.string tokenHash ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            UserId = read.uuid "user_id"
                            ClientId = read.string "client_id"
                            Scope = read.string "scope"
                            ExpiresAt = read.dateTime "expires_at"
                            FamilyId = read.uuid "family_id"
                            IsRevoked = read.bool "is_revoked"
                            ClientFingerprint = read.stringOrNone "client_fingerprint"
                        |})
                    |> Async.AwaitTask

                match result with
                | [] -> return Ok NotFound
                | [ t ] when t.IsRevoked ->
                    // Token was already used! This is a reuse attack.
                    // Return the family ID so caller can revoke entire family
                    return Ok (ReuseDetected t.FamilyId)
                | [ t ] ->
                    // Mark token as revoked (it's being used now)
                    do!
                        connectionString
                        |> Sql.connect
                        |> Sql.query "UPDATE refresh_tokens SET is_revoked = true WHERE token_hash = @token_hash"
                        |> Sql.parameters [ "token_hash", Sql.string tokenHash ]
                        |> Sql.executeNonQueryAsync
                        |> Async.AwaitTask
                        |> Async.Ignore

                    return Ok (Valid {|
                        UserId = t.UserId
                        ClientId = t.ClientId
                        Scope = t.Scope
                        ExpiresAt = t.ExpiresAt
                        FamilyId = t.FamilyId
                        ClientFingerprint = t.ClientFingerprint
                    |})
                | _ -> return Error (IdentityError.DatabaseError "Multiple tokens found")
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Legacy function for compatibility - wraps useRefreshToken
    let getAndDeleteRefreshToken (connectionString: string) (token: string) =
        async {
            let! result = useRefreshToken connectionString token
            match result with
            | Ok (Valid t) -> return Ok (Some {| UserId = t.UserId; ClientId = t.ClientId; Scope = t.Scope; ExpiresAt = t.ExpiresAt |})
            | Ok NotFound -> return Ok None
            | Ok (ReuseDetected _) -> return Error IdentityError.InvalidRefreshToken
            | Error e -> return Error e
        }

    // Token revocation
    let revokeAccessToken (connectionString: string) (token: string) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                let! rowsAffected =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM access_tokens WHERE token_hash = @token_hash"
                    |> Sql.parameters [ "token_hash", Sql.string tokenHash ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask

                return Ok (rowsAffected > 0)
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let revokeRefreshToken (connectionString: string) (token: string) =
        async {
            try
                let tokenHash = TokenGenerator.hashToken token
                let! rowsAffected =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM refresh_tokens WHERE token_hash = @token_hash"
                    |> Sql.parameters [ "token_hash", Sql.string tokenHash ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask

                return Ok (rowsAffected > 0)
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let revokeAllUserTokens (connectionString: string) (userId: Guid) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM access_tokens WHERE user_id = @user_id; DELETE FROM refresh_tokens WHERE user_id = @user_id;"
                    |> Sql.parameters [ "user_id", Sql.uuid userId ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    // Cleanup expired tokens
    let cleanupExpiredTokens (connectionString: string) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM authorization_codes WHERE expires_at < now(); DELETE FROM access_tokens WHERE expires_at < now(); DELETE FROM refresh_tokens WHERE expires_at < now();"
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }
