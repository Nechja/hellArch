namespace HellArch.CloudEvents.Files

open System

/// Published when a file is successfully uploaded.
/// Event type: hellarch.files.file.uploaded.v1
[<CLIMutable>]
type FileUploaded = {
    file_id: string
    filename: string
    mime_type: string
    size_bytes: int64
    owner_id: string
    checksum_sha256: string option
    uploaded_at: DateTimeOffset
}
