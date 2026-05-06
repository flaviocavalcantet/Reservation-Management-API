namespace Reservation.Application.Events.Reservations;

/// <summary>
/// Integration event raised when a reservation is created.
/// Published to enable downstream systems to react to reservation creation.
/// 
/// Consumers:
/// - Notification service: Send confirmation email to customer
/// - Analytics service: Track reservation trends
/// - Reporting service: Generate reports
/// </summary>
public sealed class ReservationCreatedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Unique identifier of the created reservation
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// Customer who made the reservation
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// When the reservation period starts
    /// </summary>
    public DateTime StartDateUtc { get; }

    /// <summary>
    /// When the reservation period ends
    /// </summary>
    public DateTime EndDateUtc { get; }

    /// <summary>
    /// When the reservation was created
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Initializes a new instance of the ReservationCreatedIntegrationEvent class.
    /// </summary>
    public ReservationCreatedIntegrationEvent(
        Guid reservationId,
        Guid customerId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        DateTime createdAtUtc,
        Guid correlationId = default,
        Guid causationId = default)
        : base(correlationId, causationId)
    {
        ReservationId = reservationId;
        CustomerId = customerId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the topic this event should be published to
    /// </summary>
    public override string Topic => "reservations.created";
}
