using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Reservation.Infrastructure.Identity;

/// <summary>
/// Entity Framework Core configuration for ApplicationUser.
/// 
/// This configuration applies when using a combined DbContext approach
/// (if ApplicationUser DbSet is added to ReservationDbContext).
/// 
/// Alternatively, if using separate IdentityContext, this is handled
/// in IdentityContext.OnModelCreating().
/// 
/// Configuration includes:
/// - Table naming and schema
/// - Column types and constraints
/// - Indexes for performance
/// - Default values
/// - Property mappings
/// </summary>
public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>
    /// Configures the ApplicationUser entity for PostgreSQL.
    /// </summary>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Table mapping
        builder.ToTable("AspNetUsers", schema: "public");

        // Primary key (inherited from IdentityUser, but explicitly configured)
        builder.HasKey(u => u.Id);

        // Indexes for common queries
        builder.HasIndex(u => u.Email)
            .HasName("IX_ApplicationUser_Email")
            .IsUnique();

        builder.HasIndex(u => u.UserName)
            .HasName("IX_ApplicationUser_UserName")
            .IsUnique();

        builder.HasIndex(u => u.IsActive)
            .HasName("IX_ApplicationUser_IsActive");

        builder.HasIndex(u => u.CreatedAtUtc)
            .HasName("IX_ApplicationUser_CreatedAtUtc");

        // PostgreSQL-specific column types
        builder.Property(u => u.Id)
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UserName)
            .HasColumnType("character varying(256)")
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedUserName)
            .HasColumnType("character varying(256)")
            .HasMaxLength(256);

        builder.Property(u => u.Email)
            .HasColumnType("character varying(256)")
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .HasColumnType("character varying(256)")
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .HasColumnType("character varying(256)")
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.LastLoginAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.DeactivatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.IsActive)
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(u => u.SecurityStamp)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(u => u.ConcurrencyStamp)
            .HasColumnType("text")
            .IsRequired(false)
            .IsConcurrencyToken();

        builder.Property(u => u.PhoneNumber)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(u => u.PhoneNumberConfirmed)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorEnabled)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        builder.Property(u => u.LockoutEnd)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.LockoutEnabled)
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(u => u.AccessFailedCount)
            .HasColumnType("integer")
            .HasDefaultValue(0);

        builder.Property(u => u.EmailConfirmed)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        // Relationships are managed by IdentityDbContext
        // No additional relationship configuration needed here
    }
}
