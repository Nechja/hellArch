using Microsoft.EntityFrameworkCore;
using HellArch.DataTypes.Files;

namespace Schema.Contexts;

public sealed class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<FileVariant> FileVariants => Set<FileVariant>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Bucket).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ObjectKey).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(512);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ChecksumSha256).HasMaxLength(64);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => new { e.Bucket, e.ObjectKey }).IsUnique();
            entity.HasIndex(e => e.DeletedAt).HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<FileVariant>(entity =>
        {
            entity.ToTable("file_variants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.VariantType).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ObjectKey).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(e => e.FileId);
            entity.HasOne(e => e.File)
                .WithMany(f => f.Variants)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
