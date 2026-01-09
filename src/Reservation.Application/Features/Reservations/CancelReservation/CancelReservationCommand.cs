using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Microsoft.Extensions.Logging;

namespace Reservation.Application.Features.Reservations.CancelReservation;

/// <summary>
/// Command to cancel a reservation.
/// 
/// Business Rules:
/// - Only Created or Confirmed reservations can be cancelled
/// - Confirmed reservations can only be cancelled before their start date
/// - Requires a cancellation reason
/// </summary>
public record CancelReservationCommand(
    /// <summary>ID of the reservation to cancel</summary>
    Guid ReservationId,
    
    /// <summary>Reason for cancellation</summary>
    string Reason
) : ICommand<ReservationOperationResultDto>;

/// <summary>
/// Handles CancelReservationCommand.
/// 
/// Responsibilities:
/// 1. Load the reservation aggregate
/// 2. Apply cancellation logic (enforced by domain)
/// 3. Persist state change
/// 4. Return result DTO
/// 
/// The domain aggregate enforces all business rules around cancellation timing.
/// </summary>
public class CancelReservationHandler : ICommandHandler<CancelReservationCommand, ReservationOperationResultDto>
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelReservationHandler> _logger;

    public CancelReservationHandler(
        IReservationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CancelReservationHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationOperationResultDto> Handle(
        CancelReservationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Cancelling reservation {ReservationId} with reason: {Reason}",
            command.ReservationId,
            command.Reason);

        try
        {
            // 1. Load the aggregate from repository
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);
            
            if (reservation is null)
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} not found for cancellation",
                    command.ReservationId);

                return ReservationDtoMapping.ToErrorResult(
                    $"Reservation with ID {command.ReservationId} not found.");
            }

            _logger.LogDebug(
                "Loaded reservation {ReservationId} with current status {Status} for cancellation",
                reservation.Id,
                reservation.Status);

            // 2. Apply business operation
            //    Domain enforces: can only cancel before start date if confirmed
            //    If cancellation is invalid, domain throws InvalidOperationException
            reservation.Cancel();

            // 3. Persist changes
            await _repository.UpdateAsync(reservation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} cancelled successfully",
                command.ReservationId);

            // 4. Return success DTO
            return ReservationDtoMapping.ToSuccessResult(reservation);
        }
        catch (InvalidOperationException ex)
        {
            // Domain business rule violation (e.g., trying to cancel after start date)
            _logger.LogWarning(
                ex,
                "Business rule violation when cancelling reservation {ReservationId}: {ErrorMessage}",
                command.ReservationId,
                ex.Message);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while cancelling reservation {ReservationId}",
                command.ReservationId);

            return ReservationDtoMapping.ToErrorResult(
                $"An error occurred while cancelling the reservation: {ex.Message}");
        }
    }
}
