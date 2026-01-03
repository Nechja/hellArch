namespace HellArch.DataTypes.Files

open System

/// Represents a file stored in the object storage system.
type StoredFile = {
    Id: Guid
    OwnerId: string
    Bucket: string
    ObjectKey: string
    Filename: string
    MimeType: string
    SizeBytes: int64
    ChecksumSha256: string option
    Metadata: Map<string, string> option
    CreatedAt: DateTimeOffset
    UpdatedAt: DateTimeOffset
    DeletedAt: DateTimeOffset option
    Variants: FileVariant list
}
