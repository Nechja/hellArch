using Microsoft.EntityFrameworkCore;
using HellArch.DataTypes.Identity;

namespace Schema.Contexts;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<IdentityUser> Users => Set<IdentityUser>();
    public DbSet<OAuthClient> Clients => Set<OAuthClient>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUser>(entity =>
        {
            entity.ToTable("identity_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<OAuthClient>(entity =>
        {
            entity.ToTable("oauth_clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.SecretHash).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RedirectUris).HasColumnType("text[]");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AuthorizationCode>(entity =>
        {
            entity.ToTable("authorization_codes");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(128);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RedirectUri).IsRequired();
            entity.Property(e => e.Scope).IsRequired();
            entity.Property(e => e.CodeChallenge).HasMaxLength(128);
            entity.Property(e => e.CodeChallengeMethod).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessToken>(entity =>
        {
            entity.ToTable("access_tokens");
            entity.HasKey(e => e.TokenHash);
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Scope).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.TokenHash);
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Scope).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
