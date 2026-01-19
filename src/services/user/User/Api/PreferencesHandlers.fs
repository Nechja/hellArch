namespace User.Api

open System
open Microsoft.AspNetCore.Http
open User.Domain
open User.Infrastructure
open User.Api.HttpUtils

module PreferencesHandlers =

    // Helper to verify the authenticated user owns the profile
    let private verifyOwnership (dbConfig: Database.DatabaseConfig) (profileId: Guid) (ctx: HttpContext) =
        task {
            match ctx.Request.Headers.TryGetValue("X-User-ID") with
            | false, _ -> return Error "No user ID in request"
            | true, values ->
                match Guid.TryParse(values.ToString()) with
                | false, _ -> return Error "Invalid user ID format"
                | true, authUserId ->
                    let! profileResult = Database.getProfile dbConfig profileId |> Async.StartAsTask
                    match profileResult with
                    | Error _ -> return Error "Profile not found"
                    | Ok profile ->
                        if profile.AuthUserId <> authUserId then
                            return Error "forbidden"
                        else
                            return Ok ()
        }

    let getPreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            // Verify the authenticated user owns this profile
            let! ownershipResult = verifyOwnership dbConfig userId ctx

            match ownershipResult with
            | Error "No user ID in request" -> do! writeUnauthorized ctx "No user ID in request"
            | Error "Invalid user ID format" -> do! writeBadRequest ctx "Invalid user ID format"
            | Error "forbidden" -> do! writeForbidden ctx "You can only access your own preferences"
            | Error msg -> do! writeNotFound ctx msg
            | Ok () ->
                let! result = Database.getPreferences dbConfig userId |> Async.StartAsTask

                match result with
                | Ok(Some prefs) ->
                    let dto = Mapping.UserPreferences.toDto prefs
                    do! writeJson ctx 200 dto
                | Ok None -> do! writeNotFound ctx "Preferences not found"
                | Error err -> do! handleError ctx err
        }

    let updatePreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
            // Verify the authenticated user owns this profile
            let! ownershipResult = verifyOwnership dbConfig userId ctx

            match ownershipResult with
            | Error "No user ID in request" -> do! writeUnauthorized ctx "No user ID in request"
            | Error "Invalid user ID format" -> do! writeBadRequest ctx "Invalid user ID format"
            | Error "forbidden" -> do! writeForbidden ctx "You can only update your own preferences"
            | Error msg -> do! writeNotFound ctx msg
            | Ok () ->
                let! prefsNullable = ctx.Request.ReadFromJsonAsync<HellArch.User.V1.UserPreferences>(jsonOptions)

                match Option.ofObj prefsNullable with
                | None -> do! writeBadRequest ctx "Invalid request body"
                | Some prefsDto ->
                    let prefs = Mapping.UserPreferences.fromDto userId prefsDto
                    let! result = Database.upsertPreferences dbConfig prefs |> Async.StartAsTask

                    match result with
                    | Ok updatedPrefs ->
                        let dto = Mapping.UserPreferences.toDto updatedPrefs
                        do! writeJson ctx 200 dto
                    | Error err -> do! handleError ctx err
        }
