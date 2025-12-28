namespace User.Domain

open System

/// Service-level error types
[<RequireQualifiedAccess>]
type UserError =
    | NotFound of userId: Guid
    | ValidationError of message: string
    | DatabaseError of message: string
    | Conflict of message: string

/// Result type alias for user operations
type UserResult<'T> = Result<'T, UserError>

/// Request to create a profile from an event (internal)
type CreateProfileFromEvent = {
    AuthUserId: Guid
    Email: string
    DisplayName: string option
    AvatarUrl: string option
}
