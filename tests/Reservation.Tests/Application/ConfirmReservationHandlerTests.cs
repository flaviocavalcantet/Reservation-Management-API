using Xunit;
using Moq;
using FluentAssertions;
using ReservationEntity = Reservation.Domain.Reservations.Reservation;
using Reservation.Application.Features.Reservations.ConfirmReservation;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Reservation.Tests.Builders;

namespace Reservation.Tests.Application;

/// <summary>
/// Integration tests for ConfirmReservationHandler using mocks.
/// Tests focus on confirming reservations and enforcing business rules.
/// </summary>
public class ConfirmReservationHandlerTests
{
    private readonly Mock<IReservationRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ConfirmReservationHandler _handler;

    public ConfirmReservationHandlerTests()
    {
        _mockRepository = new Mock<IReservationRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new ConfirmReservationHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithCreatedReservation_ReturnsSuccessResult()
    {
        // Arrange
        var reservation = new ReservationBuilder().Build();
        var command = new ConfirmReservationCommand(reservation.Id);

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
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Handle_WhenReservationNotFound_ReturnsErrorResult()
    {
        // Arrange
        var command = new ConfirmReservationCommand(Guid.NewGuid());

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReservationEntity?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ReturnsErrorResult()
    {
        // Arrange
        var reservation = new ReservationBuilder().BuildConfirmed();
        var command = new ConfirmReservationCommand(reservation.Id);

        _mockRepository.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
