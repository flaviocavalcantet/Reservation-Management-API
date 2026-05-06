namespace Reservation.Application.Events.Reservations;

/// <summary>
/// Integration event raised when a reservation is cancelled.
/// Published to enable downstream systems to react to reservation cancellation.
/// 
/// Consumers:
/// - Notification service: Send cancellation confirmation email to customer
/// - Inventory/Availability service: Release blocked dates in inventory system
/// - Analytics service: Track cancellation metrics
/// - Refund service: Process refunds if applicable
/// - Audit service: Record cancellation with reason
/// </summary>
public sealed class ReservationCancelledIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Unique identifier of the cancelled reservation
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// Customer who made the reservation
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// When the reservation period was scheduled to start
    /// </summary>
    public DateTime StartDateUtc { get; }

    /// <summary>
    /// When the reservation period was scheduled to end
    /// </summary>
    public DateTime EndDateUtc { get; }

    /// <summary>
    /// When the reservation was cancelled
    /// </summary>
    public DateTime CancelledAtUtc { get; }

    /// <summary>
    /// Reason for cancellation (optional, for audit trail)
    /// </summary>
    public string? CancellationReason { get; }

    /// <summary>
    /// Initializes a new instance of the ReservationCancelledIntegrationEvent class.
    /// </summary>
    public ReservationCancelledIntegrationEvent(
        Guid reservationId,
        Guid customerId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        DateTime cancelledAtUtc,
        string? cancellationReason = null,
        Guid correlationId = default,
        Guid causationId = default)
        : base(correlationId, causationId)
    {
        ReservationId = reservationId;
        CustomerId = customerId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = cancellationReason;
    }

    /// <summary>
    /// Gets the topic this event should be published to
    /// </summary>
    public override string Topic => "reservations.cancelled";
}
