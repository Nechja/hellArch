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
            | :? Npgsql.PostgresException as ex when ex.SqlState = "23505" ->
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

    let createAccessToken (connectionString: string) (token: string) (userId: Guid) (clientId: string) (scope: string) (expiresAt: DateTime) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO access_tokens (token, user_id, client_id, scope, expires_at) VALUES (@token, @user_id, @client_id, @scope, @expires_at)"
                    |> Sql.parameters [
                        "token", Sql.string token
                        "user_id", Sql.uuid userId
                        "client_id", Sql.string clientId
                        "scope", Sql.string scope
                        "expires_at", Sql.timestamptz expiresAt
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
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "SELECT user_id, client_id, scope, expires_at FROM access_tokens WHERE token = @token"
                    |> Sql.parameters [ "token", Sql.string token ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            UserId = read.uuid "user_id"
                            ClientId = read.string "client_id"
                            Scope = read.string "scope"
                            ExpiresAt = read.dateTime "expires_at"
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

    let createRefreshToken (connectionString: string) (token: string) (userId: Guid) (clientId: string) (scope: string) (expiresAt: DateTime) =
        async {
            try
                do!
                    connectionString
                    |> Sql.connect
                    |> Sql.query "INSERT INTO refresh_tokens (token, user_id, client_id, scope, expires_at) VALUES (@token, @user_id, @client_id, @scope, @expires_at)"
                    |> Sql.parameters [
                        "token", Sql.string token
                        "user_id", Sql.uuid userId
                        "client_id", Sql.string clientId
                        "scope", Sql.string scope
                        "expires_at", Sql.timestamptz expiresAt
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                    |> Async.Ignore

                return Ok ()
            with ex ->
                return Error (IdentityError.DatabaseError ex.Message)
        }

    let getAndDeleteRefreshToken (connectionString: string) (token: string) =
        async {
            try
                let! result =
                    connectionString
                    |> Sql.connect
                    |> Sql.query "DELETE FROM refresh_tokens WHERE token = @token RETURNING user_id, client_id, scope, expires_at"
                    |> Sql.parameters [ "token", Sql.string token ]
                    |> Sql.executeAsync (fun read ->
                        {|
                            UserId = read.uuid "user_id"
                            ClientId = read.string "client_id"
                            Scope = read.string "scope"
                            ExpiresAt = read.dateTime "expires_at"
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
