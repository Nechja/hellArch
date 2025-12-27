namespace User.Domain

open System

type ResultBuilder() =
    member _.Bind(x, f) =
        match x with
        | Ok v -> f v
        | Error e -> Error e

    member _.Return(x) = Ok x
    member _.ReturnFrom(x) = x

[<AutoOpen>]
module ResultBuilderModule =
    let result = ResultBuilder()

module UserProfileOperations =

    type UserError =
        | NotFound of Guid
        | ValidationError of string
        | DatabaseError of string

    let validateDisplayName (name: string option) =
        match name with
        | None -> Ok None
        | Some n when String.IsNullOrWhiteSpace(n) -> Error(ValidationError "Display name cannot be empty")
        | Some n when n.Length > 100 -> Error(ValidationError "Display name must be 100 characters or less")
        | Some n -> Ok(Some n)

    let validateBio (bio: string option) =
        match bio with
        | None -> Ok None
        | Some b when b.Length > 500 -> Error(ValidationError "Bio must be 500 characters or less")
        | Some b -> Ok(Some b)

    let validateUpdateRequest (request: UpdateProfileRequest) =
        result {
            let! displayName = validateDisplayName request.DisplayName
            let! bio = validateBio request.Bio

            return
                { DisplayName = displayName
                  AvatarUrl = request.AvatarUrl
                  Bio = bio }
        }

    let updateProfile (existing: UserProfile) (request: UpdateProfileRequest) =
        { existing with
            DisplayName = request.DisplayName |> Option.orElse existing.DisplayName
            AvatarUrl = request.AvatarUrl |> Option.orElse existing.AvatarUrl
            Bio = request.Bio |> Option.orElse existing.Bio
            UpdatedAt = DateTimeOffset.UtcNow }
