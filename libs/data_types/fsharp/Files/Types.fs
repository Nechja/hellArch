namespace HellArch.DataTypes.Files

open System

/// Represents a processed variant of a stored file (e.g., thumbnail, optimized version).
type FileVariant = {
    Id: Guid
    FileId: Guid
    VariantType: string
    ObjectKey: string
    MimeType: string
    SizeBytes: int64
    CreatedAt: DateTimeOffset
    File: StoredFile option
}

/// Represents a file stored in the object storage system.
and StoredFile = {
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

