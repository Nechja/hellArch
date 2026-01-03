namespace HellArch.CloudEvents.Files;

/// <summary>
/// Published when a file is successfully uploaded.
/// Event type: hellarch.files.file.uploaded.v1
/// </summary>
public class FileUploaded
{
    public required string FileId { get; init; }
    public required string Filename { get; init; }
    public required string MimeType { get; init; }
    public required long SizeBytes { get; init; }
    public required string OwnerId { get; init; }
    public string? ChecksumSha256 { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
}

/// <summary>
/// Published when a file is deleted.
/// Event type: hellarch.files.file.deleted.v1
/// </summary>
public class FileDeleted
{
    public required string FileId { get; init; }
    public required string Filename { get; init; }
    public required string OwnerId { get; init; }
    public required DateTimeOffset DeletedAt { get; init; }
}

/// <summary>
/// Published when file processing completes (e.g., thumbnail generation).
/// Event type: hellarch.files.file.processed.v1
/// </summary>
public class FileProcessed
{
    public required string FileId { get; init; }
    public required string VariantType { get; init; }
    public required string Status { get; init; }
    public string? Error { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
