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
