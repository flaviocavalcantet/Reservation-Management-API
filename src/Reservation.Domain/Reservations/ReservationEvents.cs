using Reservation.Domain.Abstractions;

namespace Reservation.Domain.Reservations;

/// <summary>
/// Raised when a new reservation is created.
/// Domain events record important business facts for audit trail and event-driven processing.
/// </summary>
public class ReservationCreatedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid CustomerId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public ReservationCreatedEvent(Guid reservationId, Guid customerId, DateTime startDate, DateTime endDate)
    {
        ReservationId = reservationId;
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Raised when a reservation is confirmed.
/// This could trigger side effects like sending confirmation emails, blocking calendar slots, etc.
/// </summary>
public class ReservationConfirmedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public DateTime ConfirmedAt { get; }

    public ReservationConfirmedEvent(Guid reservationId, DateTime confirmedAt)
    {
        ReservationId = reservationId;
        ConfirmedAt = confirmedAt;
    }
}

/// <summary>
/// Raised when a reservation is cancelled.
/// Could trigger cancellation notifications, refund processing, slot release, etc.
/// </summary>
public class ReservationCancelledEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public DateTime CancelledAt { get; }
    public string? Reason { get; }

    public ReservationCancelledEvent(Guid reservationId, DateTime cancelledAt, string? reason = null)
    {
        ReservationId = reservationId;
        CancelledAt = cancelledAt;
        Reason = reason;
    }
}
