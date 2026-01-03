namespace HellArch.CloudEvents.Files

open System

/// Published when a file is deleted.
/// Event type: hellarch.files.file.deleted.v1
[<CLIMutable>]
type FileDeleted = {
    file_id: string
    filename: string
    owner_id: string
    deleted_at: DateTimeOffset
}
