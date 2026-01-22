using Reservation.Domain.Abstractions;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Reservations;

/// <summary>
/// Reservation Aggregate Root - represents a reservation in the system.
/// 
/// Aggregate Root Responsibility:
/// - Acts as a consistency boundary (manages this aggregate's invariants)
/// - Enforces all business rules for reservations
/// - Manages the lifecycle: Creation → Confirmation → Cancellation
/// - Emits domain events when important things happen
/// 
/// Business Rules Enforced:
/// 1. A reservation must have valid start and end dates (end >= start)
/// 2. A confirmed reservation cannot be cancelled after its start date
/// 3. Only created reservations can be confirmed
/// 4. Only non-cancelled reservations can be cancelled
/// </summary>
public class Reservation : AggregateRoot
{
    /// <summary>
    /// The customer who made the reservation
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// When the reservation period starts
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// When the reservation period ends
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Current status of the reservation
    /// </summary>
    public ReservationStatus Status { get; private set; }

    /// <summary>
    /// When the reservation was confirmed (null if not confirmed)
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// When the reservation was cancelled (null if not cancelled)
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Reason for cancellation (if provided)
    /// </summary>
    public string? CancellationReason { get; private set; }

    // Private constructor - use factory methods for creation
    private Reservation() { }

    // ============ FACTORY METHODS ============
    // These create new aggregate instances with proper initialization

    /// <summary>
    /// Creates a new reservation.
    /// 
    /// This method:
    /// - Validates all input data (business rule enforcement)
    /// - Sets initial state
    /// - Emits a domain event to notify subscribers
    /// - Returns the new aggregate instance
    /// </summary>
    /// <param name="customerId">The ID of the customer making the reservation</param>
    /// <param name="startDate">When the reservation starts</param>
    /// <param name="endDate">When the reservation ends</param>
    /// <returns>A new Reservation aggregate</returns>
    /// <exception cref="InvalidOperationException">If dates are invalid</exception>
    public static Reservation Create(Guid customerId, DateTime startDate, DateTime endDate)
    {
        // Business Rule: EndDate must be >= StartDate
        if (endDate < startDate)
        {
            throw new DomainValidationException(
                nameof(EndDate),
                $"End date ({endDate:O}) cannot be earlier than start date ({startDate:O})");
        }

        // Business Rule: Customer ID must be valid
        if (customerId == Guid.Empty)
        {
            throw new DomainValidationException(
                nameof(CustomerId),
                "Customer ID must not be empty");
        }

        // Business Rule: Dates must not be in the past
        var now = DateTime.UtcNow;
        if (endDate <= now)
        {
            throw new BusinessRuleViolationException(
                "ReservationDateRule",
                $"Reservation end date must be in the future. Provided: {endDate:O}, Current: {now:O}");
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            Status = ReservationStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        // Emit event so subscribers know a reservation was created
        // This could trigger email notification, audit logging, etc.
        reservation.AddDomainEvent(
            new ReservationCreatedEvent(
                reservation.Id,
                customerId,
                startDate,
                endDate));

        return reservation;
    }

    // ============ BUSINESS OPERATIONS ============
    // These represent intentional actions that change reservation state

    /// <summary>
    /// Confirms a reservation.
    /// 
    /// Business Rules:
    /// - Only "Created" reservations can be confirmed
    /// - Confirmation represents system/admin approval
    /// </summary>
    /// <exception cref="InvalidOperationException">If reservation cannot be confirmed</exception>
    public void Confirm()
    {
        // Enforce business rule: only created reservations can be confirmed
        if (!Status.CanBeConfirmed)
        {
            throw new InvalidAggregateStateException(
                Status.Value,
                nameof(Confirm),
                "Only reservations in 'Created' status can be confirmed.");
        }

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;

        // Emit event so subscribers can process confirmation (send email, block calendar, etc.)
        AddDomainEvent(new ReservationConfirmedEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Cancels a reservation.
    /// 
    /// Business Rules:
    /// - Confirmed reservations cannot be cancelled after their start date
    /// - Already cancelled reservations cannot be cancelled again
    /// </summary>
    /// <param name="reason">Optional reason for cancellation (for audit trail)</param>
    /// <exception cref="InvalidOperationException">If reservation cannot be cancelled</exception>
    public void Cancel(string? reason = null)
    {
        // Enforce business rule: cannot cancel already-cancelled reservations
        if (!Status.CanBeCancelled)
        {
            throw new InvalidAggregateStateException(
                Status.Value,
                nameof(Cancel),
                "Already cancelled reservations cannot be cancelled again.");
        }

        // Enforce business rule: confirmed reservations cannot be cancelled after start date
        if (Status == ReservationStatus.Confirmed && DateTime.UtcNow >= StartDate)
        {
            throw new BusinessRuleViolationException(
                "ConfirmedReservationCancellationRule",
                $"Confirmed reservations cannot be cancelled after their start date. " +
                $"Start date: {StartDate:O}, Current time: {DateTime.UtcNow:O}");
        }

        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        ModifiedAt = DateTime.UtcNow;

        // Emit event so subscribers can process cancellation (refund, send notification, etc.)
        AddDomainEvent(new ReservationCancelledEvent(Id, DateTime.UtcNow, reason));
    }

    // ============ QUERY METHODS ============
    // These provide read access to aggregate state for business logic

    /// <summary>
    /// Determines if the reservation is currently active (not yet started).
    /// Used for business logic decisions.
    /// </summary>
    public bool IsActive => Status != ReservationStatus.Cancelled && DateTime.UtcNow < EndDate;

    /// <summary>
    /// Determines if the reservation period has started.
    /// </summary>
    public bool HasStarted => DateTime.UtcNow >= StartDate;

    /// <summary>
    /// Determines if the reservation period has ended.
    /// </summary>
    public bool HasEnded => DateTime.UtcNow >= EndDate;

    /// <summary>
    /// Gets the duration of the reservation in days.
    /// </summary>
    public int DurationDays => (int)(EndDate - StartDate).TotalDays;
}
