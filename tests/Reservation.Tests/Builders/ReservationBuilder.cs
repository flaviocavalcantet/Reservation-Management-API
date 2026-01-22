using ReservationEntity = Reservation.Domain.Reservations.Reservation;
using Reservation.Domain.Reservations;

namespace Reservation.Tests.Builders;

/// <summary>
/// Builder for creating Reservation test data with fluent API.
/// 
/// Follows the Builder pattern to make test setup cleaner and more readable.
/// Provides sensible defaults while allowing customization.
/// 
/// Usage:
/// var reservation = new ReservationBuilder()
///     .WithCustomerId(customerId)
///     .WithStartDate(DateTime.UtcNow.AddDays(1))
///     .WithStatus(ReservationStatus.Confirmed)
///     .Build();
/// </summary>
public class ReservationBuilder
{
    private Guid _customerId = Guid.NewGuid();
    private DateTime _startDate = DateTime.UtcNow.AddDays(1);
    private DateTime _endDate = DateTime.UtcNow.AddDays(6);

    public ReservationBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public ReservationBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public ReservationBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return this;
    }

    public ReservationBuilder WithDates(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
        return this;
    }

    /// <summary>
    /// Sets dates relative to now. Useful for testing time-sensitive logic.
    /// </summary>
    /// <param name="daysFromNowStart">Number of days from now for start date</param>
    /// <param name="daysFromNowEnd">Number of days from now for end date</param>
    public ReservationBuilder WithRelativeDates(int daysFromNowStart, int daysFromNowEnd)
    {
        _startDate = DateTime.UtcNow.AddDays(daysFromNowStart);
        _endDate = DateTime.UtcNow.AddDays(daysFromNowEnd);
        return this;
    }

    public ReservationEntity Build()
    {
        return ReservationEntity.Create(_customerId, _startDate, _endDate);
    }

    /// <summary>
    /// Builds a confirmed reservation.
    /// </summary>
    public ReservationEntity BuildConfirmed()
    {
        var reservation = Build();
        reservation.Confirm();
        return reservation;
    }

    /// <summary>
    /// Builds a cancelled reservation.
    /// </summary>
    public ReservationEntity BuildCancelled()
    {
        var reservation = Build();
        reservation.Cancel("Test cancellation");
        return reservation;
    }

    /// <summary>
    /// Builds a confirmed reservation that is past its start date.
    /// Useful for testing that it cannot be cancelled.
    /// </summary>
    public ReservationEntity BuildConfirmedPastStartDate()
    {
        // Use 1 second ago for start and 1 second from now for end
        // This ensures start is past but end is still future
        var now = DateTime.UtcNow;
        var reservation = new ReservationBuilder()
            .WithDates(now.AddSeconds(-1), now.AddSeconds(1))
            .Build();
        
        reservation.Confirm();
        return reservation;
    }
}
