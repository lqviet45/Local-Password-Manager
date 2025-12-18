using Microsoft.EntityFrameworkCore;
using PasswordManager.Domain.Entities;

namespace PasswordManager.Infrastructure.Repositories;

/// <summary>
/// Database context for vault storage using SQLCipher-encrypted SQLite.
/// CRITICAL: Database file is encrypted at rest using SQLCipher.
/// </summary>
public sealed class VaultDbContext : DbContext
{
    public DbSet<VaultItem> VaultItems => Set<VaultItem>();
    public DbSet<User> Users => Set<User>();

    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // VaultItem configuration
        modelBuilder.Entity<VaultItem>(entity =>
        {
            entity.ToTable("VaultItems");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(500);
            entity.Property(e => e.EncryptedData).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(5000);
            entity.Property(e => e.Tags).HasMaxLength(1000);
            entity.Property(e => e.DataHash).HasMaxLength(100);
            
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsFavorite);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.NeedsSync);
            entity.HasIndex(e => e.LastModifiedUtc);
            entity.HasIndex(e => new { e.UserId, e.IsDeleted });
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.MasterPasswordHash).IsRequired();
            entity.Property(e => e.Salt).IsRequired();
            entity.Property(e => e.EncryptedMasterKey).IsRequired();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    /// <summary>
    /// Configures SQLCipher encryption key.
    /// MUST be called before database is opened.
    /// </summary>
    public static void ConfigureSqlCipher(DbContextOptionsBuilder optionsBuilder, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        
        // SQLCipher connection string with encryption
        var connectionString = $"Data Source=vault.db;Password={password}";
        
        optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });
    }
}