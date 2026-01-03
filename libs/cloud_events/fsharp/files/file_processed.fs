namespace HellArch.CloudEvents.Files

open System

/// Published when file processing completes (e.g., thumbnail generation).
/// Event type: hellarch.files.file.processed.v1
[<CLIMutable>]
type FileProcessed = {
    file_id: string
    variant_type: string
    status: string
    error: string option
    processed_at: DateTimeOffset
}
