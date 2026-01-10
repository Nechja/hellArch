namespace User.Api

open System
open System.Text.Json
open Microsoft.AspNetCore.Http
open User.Domain
open User.Infrastructure
open User.Api.HttpUtils
open HellArch.User.V1

[<CLIMutable>]
type UpdateMeRequest = {
    DisplayName: string
    AvatarUrl: string
    Bio: string
}

module ProfileHandlers =

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
            let! bodyResult =
                task {
                    try
                        let! body = ctx.Request.ReadFromJsonAsync<CreateProfileRequest>(jsonOptions)
                        return Some body
                    with :? JsonException ->
                        return None
                }

            match bodyResult |> Option.bind Option.ofObj with
            | None -> do! writeBadRequest ctx "Invalid request body"
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
            | None -> do! writeBadRequest ctx "Invalid request body"
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

    let getMe (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            match ctx.Request.Headers.TryGetValue("X-User-ID") with
            | false, _ -> do! writeUnauthorized ctx "No user ID in request"
            | true, values ->
                match Guid.TryParse(values.ToString()) with
                | false, _ -> do! writeBadRequest ctx "Invalid user ID format"
                | true, authUserId ->
                    let! result = Database.getProfileByAuthUserId dbConfig authUserId |> Async.StartAsTask

                    match result with
                    | Ok(Some profile) ->
                        let dto = Mapping.UserProfile.toDto profile
                        do! writeJson ctx 200 dto
                    | Ok None -> do! writeNotFound ctx "Profile not found"
                    | Error err -> do! handleError ctx err
        }

    let updateMe (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            match ctx.Request.Headers.TryGetValue("X-User-ID") with
            | false, _ -> do! writeUnauthorized ctx "No user ID in request"
            | true, values ->
                match Guid.TryParse(values.ToString()) with
                | false, _ -> do! writeBadRequest ctx "Invalid user ID format"
                | true, authUserId ->
                    let! profileResult = Database.getProfileByAuthUserId dbConfig authUserId |> Async.StartAsTask

                    match profileResult with
                    | Ok(Some profile) ->
                        let! bodyNullable = JsonSerializer.DeserializeAsync<UpdateMeRequest>(ctx.Request.Body, jsonOptions)

                        match Option.ofObj bodyNullable with
                        | None -> do! writeBadRequest ctx "Invalid request body"
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
                    | Ok None -> do! writeNotFound ctx "Profile not found"
                    | Error err -> do! handleError ctx err
        }
