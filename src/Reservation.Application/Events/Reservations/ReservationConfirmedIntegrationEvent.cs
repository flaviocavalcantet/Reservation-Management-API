namespace Reservation.Application.Events.Reservations;

/// <summary>
/// Integration event raised when a reservation is confirmed.
/// Published to enable downstream systems to react to reservation confirmation.
/// 
/// Consumers:
/// - Notification service: Send confirmation email to customer
/// - Inventory/Availability service: Block dates in inventory system
/// - Analytics service: Track confirmed reservations
/// - Payment service: Initiate payment collection if needed
/// </summary>
public sealed class ReservationConfirmedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Unique identifier of the confirmed reservation
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
    /// When the reservation was confirmed
    /// </summary>
    public DateTime ConfirmedAtUtc { get; }

    /// <summary>
    /// Initializes a new instance of the ReservationConfirmedIntegrationEvent class.
    /// </summary>
    public ReservationConfirmedIntegrationEvent(
        Guid reservationId,
        Guid customerId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        DateTime confirmedAtUtc,
        Guid correlationId = default,
        Guid causationId = default)
        : base(correlationId, causationId)
    {
        ReservationId = reservationId;
        CustomerId = customerId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        ConfirmedAtUtc = confirmedAtUtc;
    }

    /// <summary>
    /// Gets the topic this event should be published to
    /// </summary>
    public override string Topic => "reservations.confirmed";
}
