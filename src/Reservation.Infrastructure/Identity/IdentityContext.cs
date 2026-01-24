using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Reservation.Infrastructure.Identity;

/// <summary>
/// Identity-specific DbContext for managing Identity entities.
/// 
/// This separate context serves two purposes:
/// 1. Keeps Identity tables isolated from domain models
/// 2. Allows for Identity-specific configurations and migrations
/// 
/// Design Notes:
/// - Extends IdentityDbContext{ApplicationUser, IdentityRole{Guid}, Guid}
/// - Automatically creates all Identity tables (Users, Roles, Claims, etc.)
/// - Configured for PostgreSQL
/// - Can be combined with ReservationDbContext if single-context approach is preferred
/// 
/// Alternative Approach:
/// If preferring a single DbContext, ApplicationUser DbSet can be added directly to
/// ReservationDbContext instead of using this separate context.
/// </summary>
public class IdentityContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new instance of IdentityContext with EF Core options.
    /// </summary>
    /// <param name="options">DbContext options for PostgreSQL configuration.</param>
    public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the Identity model and applies custom entity configurations.
    /// 
    /// This method:
    /// - Calls base configuration to set up Identity tables
    /// - Applies any custom entity configurations from the assembly
    /// - Sets up constraints and indexes
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply custom Identity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);

        // Configure PostgreSQL-specific table naming
        // This ensures tables are created with proper PostgreSQL conventions
        modelBuilder.HasDefaultSchema("public");

        // ApplicationUser custom configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            // Table naming
            entity.ToTable("AspNetUsers", schema: "public");

            // Index for email (case-insensitive in PostgreSQL)
            entity.HasIndex(u => u.Email)
                .HasName("IX_AspNetUsers_Email")
                .IsUnique();

            // Index for username (case-insensitive in PostgreSQL)
            entity.HasIndex(u => u.UserName)
                .HasName("IX_AspNetUsers_UserName")
                .IsUnique();

            // Index for active users (for queries filtering inactive accounts)
            entity.HasIndex(u => u.IsActive)
                .HasName("IX_AspNetUsers_IsActive");

            // Property configurations
            entity.Property(u => u.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            entity.Property(u => u.UserName)
                .HasMaxLength(256);

            entity.Property(u => u.Email)
                .HasMaxLength(256);

            entity.Property(u => u.FullName)
                .HasMaxLength(256)
                .IsRequired(false);

            entity.Property(u => u.CreatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.LastLoginAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            entity.Property(u => u.DeactivatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);
        });

        // Configure Identity Role table
        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoles", schema: "public");

            entity.Property(r => r.Id)
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            entity.Property(r => r.Name)
                .HasMaxLength(256);

            entity.HasIndex(r => r.NormalizedName)
                .HasName("IX_AspNetRoles_NormalizedName")
                .IsUnique();
        });

        // Configure Identity UserRole table
        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserRoles", schema: "public");

            entity.Property(ur => ur.UserId)
                .HasColumnType("uuid");

            entity.Property(ur => ur.RoleId)
                .HasColumnType("uuid");
        });

        // Configure Identity UserClaim table
        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserClaims", schema: "public");

            entity.Property(uc => uc.UserId)
                .HasColumnType("uuid");
        });

        // Configure Identity UserLogin table
        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserLogins", schema: "public");

            entity.Property(ul => ul.UserId)
                .HasColumnType("uuid");
        });

        // Configure Identity RoleClaim table
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims", schema: "public");

            entity.Property(rc => rc.RoleId)
                .HasColumnType("uuid");
        });

        // Configure Identity UserToken table
        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("AspNetUserTokens", schema: "public");

            entity.Property(ut => ut.UserId)
                .HasColumnType("uuid");
        });
    }
}
