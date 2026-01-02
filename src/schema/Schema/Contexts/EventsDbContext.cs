using Microsoft.EntityFrameworkCore;
using HellArch.DataTypes.Events;

namespace Schema.Contexts;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ActorId).HasMaxLength(255);
            entity.Property(e => e.TargetId).HasMaxLength(255);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.ActorId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.ToTable("webhooks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventTypes).HasColumnType("text[]");
            entity.Property(e => e.Format).HasConversion<string>().HasMaxLength(50).HasDefaultValue(WebhookFormat.Raw);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.ToTable("webhook_deliveries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EventId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.ResponseBody).HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.WebhookId);
            entity.HasIndex(e => e.EventId);
            entity.HasOne(e => e.Webhook).WithMany(w => w.Deliveries).HasForeignKey(e => e.WebhookId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
