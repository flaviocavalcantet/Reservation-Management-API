using Reservation.Domain.Abstractions;

namespace Reservation.Domain.Reservations;

/// <summary>
/// ReservationStatus is a Value Object representing the lifecycle state of a reservation.
/// 
/// Value Objects in DDD:
/// - Are immutable (cannot be changed after creation)
/// - Have no identity (compared by value, not reference)
/// - Encapsulate domain logic and validation for the concept they represent
/// 
/// This approach provides type safety and makes invalid states impossible to represent.
/// </summary>
public class ReservationStatus : ValueObject
{
    /// <summary>
    /// The string representation of the status
    /// </summary>
    public string Value { get; }

    // Private constructor - enforce creation through factory methods
    private ReservationStatus(string value)
    {
        Value = value;
    }

    // ============ FACTORY METHODS ============
    // These ensure only valid statuses can be created

    /// <summary>
    /// Creates a "Created" status - initial state of a new reservation
    /// </summary>
    public static ReservationStatus Created => new(nameof(Created));

    /// <summary>
    /// Creates a "Confirmed" status - reservation has been confirmed by admin/system
    /// </summary>
    public static ReservationStatus Confirmed => new(nameof(Confirmed));

    /// <summary>
    /// Creates a "Cancelled" status - reservation has been cancelled
    /// </summary>
    public static ReservationStatus Cancelled => new(nameof(Cancelled));

    // ============ VALIDATION METHODS ============
    // Encapsulates business rules about what transitions are valid

    /// <summary>
    /// Determines if this status can be confirmed.
    /// Only "Created" status can be confirmed.
    /// </summary>
    public bool CanBeConfirmed => Value == nameof(Created);

    /// <summary>
    /// Determines if this status can be cancelled.
    /// "Cancelled" status cannot be changed; it's a terminal state.
    /// </summary>
    public bool CanBeCancelled => Value != nameof(Cancelled);

    // ============ EQUALITY IMPLEMENTATION ============
    // Value Objects are compared by their values, not by reference

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    /// <summary>
    /// String representation for debugging and logging
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Parses a string to a ReservationStatus.
    /// Useful for database deserialization and API input validation.
    /// </summary>
    public static ReservationStatus FromString(string status) => status switch
    {
        nameof(Created) => Created,
        nameof(Confirmed) => Confirmed,
        nameof(Cancelled) => Cancelled,
        _ => throw new InvalidOperationException($"Invalid reservation status: '{status}'")
    };
}
