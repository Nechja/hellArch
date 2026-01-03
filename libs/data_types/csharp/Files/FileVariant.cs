namespace HellArch.DataTypes.Files;

/// <summary>
/// Represents a processed variant of a stored file (e.g., thumbnail, optimized version).
/// </summary>
public class FileVariant
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public required string VariantType { get; set; }
    public required string ObjectKey { get; set; }
    public required string MimeType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties (EF Core)
    public StoredFile? File { get; set; }
}
