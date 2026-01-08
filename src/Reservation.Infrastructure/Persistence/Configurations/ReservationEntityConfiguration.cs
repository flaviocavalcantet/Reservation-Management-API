using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Reservations;

namespace Reservation.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for the Reservation aggregate root.
/// 
/// Configures:
/// - Table mapping and primary key
/// - Column types and constraints
/// - Value object mappings (ReservationStatus)
/// - Indexes for query optimization
/// - Shadow properties if needed
/// 
/// This configuration ensures the database schema matches domain semantics.
/// </summary>
public class ReservationEntityConfiguration : IEntityTypeConfiguration<Domain.Reservations.Reservation>
{
    public void Configure(EntityTypeBuilder<Domain.Reservations.Reservation> builder)
    {
        // ============ TABLE AND KEY CONFIGURATION ============
        builder.ToTable("Reservations", schema: "public");
        builder.HasKey(r => r.Id);

        // ============ PROPERTY CONFIGURATION ============
        
        // CustomerId - required foreign key reference
        builder.Property(r => r.CustomerId)
            .IsRequired()
            .HasColumnType("uuid");

        // StartDate - required timestamp
        builder.Property(r => r.StartDate)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        // EndDate - required timestamp
        builder.Property(r => r.EndDate)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        // Status - value object stored as string (status name)
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion(
                status => status.Name,  // Convert to string for database storage
                name => ReservationStatus.FromName(name))  // Convert back from string
            .HasColumnType("varchar(50)")
            .HasColumnName("Status");

        // Created and Modified timestamps (from AggregateRoot base)
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.ModifiedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ============ INDEXES FOR QUERY OPTIMIZATION ============
        
        // Optimize queries by customer ID - common access pattern
        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("idx_reservations_customer_id");

        // Optimize queries by date range
        builder.HasIndex(r => new { r.StartDate, r.EndDate })
            .HasDatabaseName("idx_reservations_dates");

        // Optimize queries filtering by status and customer
        builder.HasIndex(r => new { r.CustomerId, r.Status })
            .HasDatabaseName("idx_reservations_customer_status");

        // ============ DOMAIN EVENT HANDLING ============
        // Note: Domain events are not persisted in this design.
        // They are published through IDomainEventPublisher during command handling.
        // If you need event sourcing, add an events table here.

        // ============ SHADOW PROPERTIES ============
        // None needed for basic implementation
        // Could add: RowVersion for optimistic concurrency control if needed
    }
}
