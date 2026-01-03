namespace HellArch.DataTypes.Files;

/// <summary>
/// Represents a file stored in the object storage system.
/// </summary>
public class StoredFile
{
    public Guid Id { get; set; }
    public required string OwnerId { get; set; }
    public required string Bucket { get; set; }
    public required string ObjectKey { get; set; }
    public required string Filename { get; set; }
    public required string MimeType { get; set; }
    public long SizeBytes { get; set; }
    public string? ChecksumSha256 { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties (EF Core)
    public ICollection<FileVariant> Variants { get; set; } = [];
}
