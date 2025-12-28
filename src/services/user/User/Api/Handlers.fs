namespace User.Api

open System
open System.Text.Json
open Microsoft.AspNetCore.Http
open User.Domain
open User.Infrastructure
open HellArch.User.V1

[<CLIMutable>]
type UpdateMeRequest = {
    DisplayName: string
    AvatarUrl: string
    Bio: string
}

module Handlers =

    let private jsonOptions =
        let options =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)

        options.WriteIndented <- true
        options

    let private writeJson (ctx: HttpContext) (statusCode: int) (data: obj) =
        task {
            ctx.Response.StatusCode <- statusCode
            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions))
        }

    let private handleError (ctx: HttpContext) (error: UserError) =
        match error with
        | UserError.NotFound id -> writeJson ctx 404 {| error = "not_found"; message = $"User {id} not found" |}
        | UserError.ValidationError msg -> writeJson ctx 400 {| error = "validation_error"; message = msg |}
        | UserError.DatabaseError _ -> writeJson ctx 500 {| error = "internal_error"; message = "Database error" |}
        | UserError.Conflict msg -> writeJson ctx 409 {| error = "conflict"; message = msg |}

    let getProfile (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! result = Database.getProfile dbConfig userId |> Async.StartAsTask

            match result with
            | Ok profile ->
                let dto = Mapping.UserProfile.toDto profile
                do! writeJson ctx 200 dto
            | Error err -> do! handleError ctx err
        }

    let createProfile (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            let! bodyNullable = ctx.Request.ReadFromJsonAsync<CreateProfileRequest>(jsonOptions)

            match Option.ofObj bodyNullable with
            | None ->
                do!
                    writeJson
                        ctx
                        400
                        {|
                            error = "bad_request"
                            message = "Invalid request body"
                        |}
            | Some body ->
                let request: CreateProfileFromEvent =
                    {
                        AuthUserId = Guid.Parse(body.UserId)
                        Email = body.Email
                        DisplayName =
                            if String.IsNullOrEmpty body.DisplayName then
                                None
                            else
                                Some body.DisplayName
                        AvatarUrl =
                            if String.IsNullOrEmpty body.AvatarUrl then
                                None
                            else
                                Some body.AvatarUrl
                    }

                let! result = Database.createProfile dbConfig request |> Async.StartAsTask

                match result with
                | Ok profile ->
                    let dto = Mapping.UserProfile.toDto profile
                    do! writeJson ctx 201 dto
                | Error err -> do! handleError ctx err
        }

    let updateProfile (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! bodyNullable = ctx.Request.ReadFromJsonAsync<UpdateProfileRequest>(jsonOptions)

            match Option.ofObj bodyNullable with
            | None ->
                do!
                    writeJson
                        ctx
                        400
                        {|
                            error = "bad_request"
                            message = "Invalid request body"
                        |}
            | Some body ->
                let displayName = body.DisplayName
                let avatarUrl = body.AvatarUrl
                let bio = body.Bio

                let! result =
                    Database.updateProfile dbConfig userId displayName avatarUrl bio
                    |> Async.StartAsTask

                match result with
                | Ok profile ->
                    let dto = Mapping.UserProfile.toDto profile
                    do! writeJson ctx 200 dto
                | Error err -> do! handleError ctx err
        }

    let getPreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! result = Database.getPreferences dbConfig userId |> Async.StartAsTask

            match result with
            | Ok(Some prefs) ->
                let dto = Mapping.UserPreferences.toDto prefs
                do! writeJson ctx 200 dto
            | Ok None -> do! writeJson ctx 404 {| error = "not_found"; message = "Preferences not found" |}
            | Error err -> do! handleError ctx err
        }

    let updatePreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! prefsNullable = ctx.Request.ReadFromJsonAsync<HellArch.User.V1.UserPreferences>(jsonOptions)

            match Option.ofObj prefsNullable with
            | None ->
                do!
                    writeJson
                        ctx
                        400
                        {|
                            error = "bad_request"
                            message = "Invalid request body"
                        |}
            | Some prefsDto ->
                let prefs = Mapping.UserPreferences.fromDto userId prefsDto
                let! result = Database.upsertPreferences dbConfig prefs |> Async.StartAsTask

                match result with
                | Ok updatedPrefs ->
                    let dto = Mapping.UserPreferences.toDto updatedPrefs
                    do! writeJson ctx 200 dto
                | Error err -> do! handleError ctx err
        }

    let health (ctx: HttpContext) = task { do! writeJson ctx 200 {| status = "healthy" |} }

    let getMe (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            match ctx.Request.Headers.TryGetValue("X-User-ID") with
            | false, _ ->
                do! writeJson ctx 401 {| error = "unauthorized"; message = "No user ID in request" |}
            | true, values ->
                match Guid.TryParse(values.ToString()) with
                | false, _ ->
                    do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid user ID format" |}
                | true, authUserId ->
                    let! result = Database.getProfileByAuthUserId dbConfig authUserId |> Async.StartAsTask

                    match result with
                    | Ok(Some profile) ->
                        let dto = Mapping.UserProfile.toDto profile
                        do! writeJson ctx 200 dto
                    | Ok None ->
                        do! writeJson ctx 404 {| error = "not_found"; message = "Profile not found" |}
                    | Error err -> do! handleError ctx err
        }

    let updateMe (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            match ctx.Request.Headers.TryGetValue("X-User-ID") with
            | false, _ ->
                do! writeJson ctx 401 {| error = "unauthorized"; message = "No user ID in request" |}
            | true, values ->
                match Guid.TryParse(values.ToString()) with
                | false, _ ->
                    do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid user ID format" |}
                | true, authUserId ->
                    let! profileResult = Database.getProfileByAuthUserId dbConfig authUserId |> Async.StartAsTask

                    match profileResult with
                    | Ok(Some profile) ->
                        let! bodyNullable = JsonSerializer.DeserializeAsync<UpdateMeRequest>(ctx.Request.Body, jsonOptions)

                        match Option.ofObj bodyNullable with
                        | None ->
                            do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid request body" |}
                        | Some body ->
                            let displayName = if String.IsNullOrEmpty body.DisplayName then None else Some body.DisplayName
                            let avatarUrl = if String.IsNullOrEmpty body.AvatarUrl then None else Some body.AvatarUrl
                            let bio = if String.IsNullOrEmpty body.Bio then None else Some body.Bio

                            let! result =
                                Database.updateProfile dbConfig profile.Id displayName avatarUrl bio
                                |> Async.StartAsTask

                            match result with
                            | Ok updated ->
                                let dto = Mapping.UserProfile.toDto updated
                                do! writeJson ctx 200 dto
                            | Error err -> do! handleError ctx err
                    | Ok None ->
                        do! writeJson ctx 404 {| error = "not_found"; message = "Profile not found" |}
                    | Error err -> do! handleError ctx err
        }
