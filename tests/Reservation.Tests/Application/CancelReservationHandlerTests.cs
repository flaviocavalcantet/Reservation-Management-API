using Xunit;
using Moq;
using FluentAssertions;
using ReservationEntity = Reservation.Domain.Reservations.Reservation;
using Reservation.Application.Features.Reservations.CancelReservation;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Reservation.Tests.Builders;

namespace Reservation.Tests.Application;

/// <summary>
/// Integration tests for CancelReservationHandler using mocks.
/// Tests focus on cancelling reservations and enforcing business rules.
/// </summary>
public class CancelReservationHandlerTests
{
    private readonly Mock<IReservationRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CancelReservationHandler _handler;

    public CancelReservationHandlerTests()
    {
        _mockRepository = new Mock<IReservationRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new CancelReservationHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithCreatedReservation_ReturnsSuccessResult()
    {
        // Arrange
        var reservation = new ReservationBuilder().Build();
        var command = new CancelReservationCommand(reservation.Id, "Customer requested");

        _mockRepository.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ReservationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Handle_WhenReservationNotFound_ReturnsErrorResult()
    {
        // Arrange
        var command = new CancelReservationCommand(Guid.NewGuid(), "Customer requested");

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReservationEntity?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenAlreadyCancelled_ReturnsErrorResult()
    {
        // Arrange
        var reservation = new ReservationBuilder().BuildCancelled();
        var command = new CancelReservationCommand(reservation.Id, "Customer requested");

        _mockRepository.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already cancelled");
    }

    [Fact]
    public async Task Handle_ConfirmedAfterStartDate_ReturnsErrorResult()
    {
        // Arrange
        var reservation = new ReservationBuilder().BuildConfirmedPastStartDate();
        var command = new CancelReservationCommand(reservation.Id, "Customer requested");

        _mockRepository.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("after its start date");
    }
}
