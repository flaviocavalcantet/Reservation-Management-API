using Xunit;
using FluentAssertions;
using ReservationEntity = Reservation.Domain.Reservations.Reservation;
using Reservation.Domain.Reservations;

namespace Reservation.Tests.Domain;

/// <summary>
/// Unit tests for the Reservation aggregate root.
/// 
/// Tests focus on:
/// - Business rule enforcement during creation
/// - State transitions (Confirm, Cancel)
/// - Invalid operation prevention
/// - Domain event emission
/// 
/// Pattern: Arrange / Act / Assert
/// </summary>
public class ReservationTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidDates_ReturnsReservation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddDays(5);

        // Act
        var reservation = ReservationEntity.Create(customerId, startDate, endDate);

        // Assert
        reservation.Should().NotBeNull();
        reservation.CustomerId.Should().Be(customerId);
        reservation.StartDate.Should().Be(startDate);
        reservation.EndDate.Should().Be(endDate);
        reservation.Status.Should().Be(ReservationStatus.Created);
        reservation.Id.Should().NotBe(Guid.Empty);
        reservation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithSameDates_ReturnsReservation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(1);

        // Act
        var reservation = ReservationEntity.Create(customerId, date, date);

        // Assert - same start and end dates should be valid (zero-length reservation)
        reservation.Should().NotBeNull();
        reservation.StartDate.Should().Be(date);
        reservation.EndDate.Should().Be(date);
        reservation.Status.Should().Be(ReservationStatus.Created);
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(5);
        var endDate = DateTime.UtcNow.AddDays(1); // Before start date

        // Act
        var action = () => ReservationEntity.Create(customerId, startDate, endDate);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*end date*cannot be earlier than start date*");
    }

    [Fact]
    public void Create_WithValidData_EmitsDomainEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddDays(5);

        // Act
        var reservation = ReservationEntity.Create(customerId, startDate, endDate);

        // Assert
        reservation.GetDomainEvents().Should().HaveCount(1);
        var createdEvent = reservation.GetDomainEvents().First() as ReservationCreatedEvent;
        createdEvent.Should().NotBeNull();
        createdEvent!.ReservationId.Should().Be(reservation.Id);
        createdEvent.CustomerId.Should().Be(customerId);
        createdEvent.StartDate.Should().Be(startDate);
        createdEvent.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void Create_WithPastStartDate_ReturnsReservation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-1); // Past date
        var endDate = DateTime.UtcNow.AddDays(5);

        // Act
        var reservation = ReservationEntity.Create(customerId, startDate, endDate);

        // Assert - past dates should be allowed (for testing flexibility)
        reservation.Should().NotBeNull();
        reservation.StartDate.Should().Be(startDate);
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_WhenCreated_TransitionsToConfirmedStatus()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Act
        reservation.Confirm();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_WhenCreated_EmitsDomainEvent()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Clear creation event
        reservation.ClearDomainEvents();

        // Act
        reservation.Confirm();

        // Assert
        reservation.GetDomainEvents().Should().HaveCount(1);
        var confirmedEvent = reservation.GetDomainEvents().First() as ReservationConfirmedEvent;
        confirmedEvent.Should().NotBeNull();
        confirmedEvent!.ReservationId.Should().Be(reservation.Id);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));
        reservation.Confirm();

        // Act
        var action = () => reservation.Confirm();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot confirm*Confirmed*status*");
    }

    [Fact]
    public void Confirm_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));
        reservation.Cancel("Testing");

        // Act
        var action = () => reservation.Confirm();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot confirm*Cancelled*status*");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_WhenCreated_TransitionsToCancelledStatus()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Act
        reservation.Cancel("Customer request");

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenConfirmedBeforeStartDate_TransitionsToCancelledStatus()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(5); // 5 days in the future
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            startDate,
            startDate.AddDays(3));
        reservation.Confirm();

        // Act
        reservation.Cancel("Customer request");

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenConfirmedAfterStartDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1); // Past date (before now)
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            startDate,
            startDate.AddDays(3));
        reservation.Confirm();

        // Act
        var action = () => reservation.Cancel("Customer request");

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a confirmed reservation after its start date*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));
        reservation.Cancel("First cancellation");

        // Act
        var action = () => reservation.Cancel("Second cancellation");

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a reservation that is already cancelled*");
    }

    [Fact]
    public void Cancel_WhenCreated_EmitsDomainEvent()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Clear creation event
        reservation.ClearDomainEvents();

        // Act
        reservation.Cancel("Customer request");

        // Assert
        reservation.GetDomainEvents().Should().HaveCount(1);
        var cancelledEvent = reservation.GetDomainEvents().First() as ReservationCancelledEvent;
        cancelledEvent.Should().NotBeNull();
        cancelledEvent!.ReservationId.Should().Be(reservation.Id);
        cancelledEvent.Reason.Should().Be("Customer request");
    }

    [Fact]
    public void Cancel_WithoutReason_EmitsDomainEventWithoutReason()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Clear creation event
        reservation.ClearDomainEvents();

        // Act
        reservation.Cancel(); // No reason provided

        // Assert
        reservation.GetDomainEvents().Should().HaveCount(1);
        var cancelledEvent = reservation.GetDomainEvents().First() as ReservationCancelledEvent;
        cancelledEvent.Should().NotBeNull();
        cancelledEvent!.Reason.Should().BeNull();
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void StatusTransition_CreatedToConfirmedToCancel_AllowsTransition()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(5);
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            startDate,
            startDate.AddDays(3));

        // Act & Assert - Created to Confirmed
        reservation.Status.Should().Be(ReservationStatus.Created);
        reservation.Confirm();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);

        // Act & Assert - Confirmed to Cancelled (before start date)
        reservation.Cancel("Customer request");
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void StatusTransition_CreatedDirectlyToCancelled_AllowsTransition()
    {
        // Arrange
        var reservation = ReservationEntity.Create(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(5));

        // Act
        reservation.Cancel("Changed mind");

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    #endregion

    #region Business Rule Tests

    [Fact]
    public void ReservationStatus_CreatedCanBeConfirmed()
    {
        // Arrange
        var status = ReservationStatus.Created;

        // Act & Assert
        status.CanBeConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ReservationStatus_ConfirmedCannotBeConfirmed()
    {
        // Arrange
        var status = ReservationStatus.Confirmed;

        // Act & Assert
        status.CanBeConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ReservationStatus_CancelledCannotBeConfirmed()
    {
        // Arrange
        var status = ReservationStatus.Cancelled;

        // Act & Assert
        status.CanBeConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ReservationStatus_CreatedCanBeCancelled()
    {
        // Arrange
        var status = ReservationStatus.Created;

        // Act & Assert
        status.CanBeCancelled.Should().BeTrue();
    }

    [Fact]
    public void ReservationStatus_ConfirmedCanBeCancelled()
    {
        // Arrange
        var status = ReservationStatus.Confirmed;

        // Act & Assert
        status.CanBeCancelled.Should().BeTrue();
    }

    [Fact]
    public void ReservationStatus_CancelledCannotBeCancelled()
    {
        // Arrange
        var status = ReservationStatus.Cancelled;

        // Act & Assert
        status.CanBeCancelled.Should().BeFalse();
    }

    #endregion

    #region Value Object Equality Tests

    [Fact]
    public void ReservationStatus_SameValue_AreEqual()
    {
        // Arrange
        var status1 = ReservationStatus.Created;
        var status2 = ReservationStatus.Created;

        // Act & Assert
        status1.Should().Be(status2);
    }

    [Fact]
    public void ReservationStatus_DifferentValues_AreNotEqual()
    {
        // Arrange
        var status1 = ReservationStatus.Created;
        var status2 = ReservationStatus.Confirmed;

        // Act & Assert
        status1.Should().NotBe(status2);
    }

    #endregion
}
