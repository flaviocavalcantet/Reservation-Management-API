using Xunit;
using Moq;
using FluentAssertions;
using ReservationEntity = Reservation.Domain.Reservations.Reservation;
using Reservation.Application.Features.Reservations.CreateReservation;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;

namespace Reservation.Tests.Application;

/// <summary>
/// Integration tests for CreateReservationHandler using mocks.
/// Tests focus on command handler business logic, repository interaction, and error handling.
/// </summary>
public class CreateReservationHandlerTests
{
    private readonly Mock<IReservationRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateReservationHandler _handler;

    public CreateReservationHandlerTests()
    {
        _mockRepository = new Mock<IReservationRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateReservationHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddDays(5);
        var command = new CreateReservationCommand(customerId, startDate, endDate);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ReservationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ReservationId.Should().NotBe(Guid.Empty);
        result.Status.Should().Be("Created");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsRepositoryAndUnitOfWork()
    {
        // Arrange
        var command = new CreateReservationCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ReservationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ReservationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEndDateBeforeStartDate_ReturnsErrorResult()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(5);
        var endDate = DateTime.UtcNow.AddDays(1);
        var command = new CreateReservationCommand(Guid.NewGuid(), startDate, endDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidDates_DoesNotPersist()
    {
        // Arrange
        var command = new CreateReservationCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ReservationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
