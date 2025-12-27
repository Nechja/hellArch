namespace User.Api

open System
open System.Text.Json
open Microsoft.AspNetCore.Http
open User.Domain
open User.Infrastructure

module Handlers =

    let private jsonOptions =
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)
        options.WriteIndented <- true
        options

    let private writeJson (ctx: HttpContext) (statusCode: int) (data: obj) =
        task {
            ctx.Response.StatusCode <- statusCode
            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions))
        }

    let private handleError (ctx: HttpContext) (error: UserProfileOperations.UserError) =
        match error with
        | UserProfileOperations.NotFound id ->
            writeJson ctx 404 {| error = "not_found"; message = $"User {id} not found" |}
        | UserProfileOperations.ValidationError msg ->
            writeJson ctx 400 {| error = "validation_error"; message = msg |}
        | UserProfileOperations.DatabaseError _ ->
            writeJson ctx 500 {| error = "internal_error"; message = "Database error" |}

    let getProfile (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! result = Database.getProfileOption dbConfig userId |> Async.StartAsTask

            match result with
            | Ok(Some profile) -> do! writeJson ctx 200 profile
            | Ok None -> do! writeJson ctx 404 {| error = "not_found"; message = "Profile not found" |}
            | Error err -> do! handleError ctx err
        }

    let createProfile (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            let! bodyNullable = ctx.Request.ReadFromJsonAsync<CreateProfileRequest>()

            match Option.ofObj bodyNullable with
            | None -> do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid request body" |}
            | Some body ->
                let! result = Database.createProfile dbConfig body |> Async.StartAsTask

                match result with
                | Ok profile -> do! writeJson ctx 201 profile
                | Error err -> do! handleError ctx err
        }

    let updateProfile (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! bodyNullable = ctx.Request.ReadFromJsonAsync<UpdateProfileRequest>()

            match Option.ofObj bodyNullable with
            | None -> do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid request body" |}
            | Some body ->
                match UserProfileOperations.validateUpdateRequest body with
                | Error err -> do! handleError ctx err
                | Ok validatedRequest ->
                    let! result = Database.updateProfile dbConfig userId validatedRequest |> Async.StartAsTask

                    match result with
                    | Ok profile -> do! writeJson ctx 200 profile
                    | Error err -> do! handleError ctx err
        }

    let getPreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! result = Database.getPreferences dbConfig userId |> Async.StartAsTask

            match result with
            | Ok prefs -> do! writeJson ctx 200 prefs
            | Error err -> do! handleError ctx err
        }

    let updatePreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            let! prefsNullable = ctx.Request.ReadFromJsonAsync<UserPreferences>()

            match Option.ofObj prefsNullable with
            | None -> do! writeJson ctx 400 {| error = "bad_request"; message = "Invalid request body" |}
            | Some prefs ->
                let! result = Database.upsertPreferences dbConfig userId prefs |> Async.StartAsTask

                match result with
                | Ok updatedPrefs -> do! writeJson ctx 200 updatedPrefs
                | Error err -> do! handleError ctx err
        }

    let health (ctx: HttpContext) =
        task { do! writeJson ctx 200 {| status = "healthy" |} }
