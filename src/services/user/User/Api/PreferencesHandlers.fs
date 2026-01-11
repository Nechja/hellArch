namespace User.Api

open System
open Microsoft.AspNetCore.Http
open User.Domain
open User.Infrastructure
open User.Api.HttpUtils

module PreferencesHandlers =

    let getPreferences (dbConfig: Database.DatabaseConfig) (userId: Guid) (ctx: HttpContext) =
        task {
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
