using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Exceptions;
using Reservation.Domain.Reservations;
using Microsoft.Extensions.Logging;

namespace Reservation.Application.Features.Reservations.ConfirmReservation;

/// <summary>
/// Command to confirm a reservation (transition from Created to Confirmed status).
/// </summary>
public record ConfirmReservationCommand(
    /// <summary>ID of the reservation to confirm</summary>
    Guid ReservationId
) : ICommand<ReservationOperationResultDto>;

/// <summary>
/// Handles ConfirmReservationCommand.
/// 
/// Responsibilities:
/// 1. Load the reservation aggregate
/// 2. Apply business rule validation
/// 3. Transition to Confirmed status
/// 4. Persist changes
/// 5. Return result DTO
/// 
/// Business Rule: Only reservations in Created status can be confirmed
/// </summary>
public class ConfirmReservationHandler : ICommandHandler<ConfirmReservationCommand, ReservationOperationResultDto>
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmReservationHandler> _logger;

    public ConfirmReservationHandler(
        IReservationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmReservationHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationOperationResultDto> Handle(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Confirming reservation {ReservationId}",
            command.ReservationId);

        try
        {
            // 1. Load the aggregate from repository
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);
            
            if (reservation is null)
            {
                _logger.LogWarning(
                    "Attempted to confirm non-existent reservation {ReservationId}",
                    command.ReservationId);

                throw new AggregateNotFoundException(nameof(Reservation), command.ReservationId);
            }

            _logger.LogDebug(
                "Loaded reservation {ReservationId} with status {Status}",
                reservation.Id,
                reservation.Status);

            // 2. Apply business operation (enforces transition rules in domain)
            reservation.Confirm();

            // 3. Persist changes
            await _repository.UpdateAsync(reservation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} confirmed successfully",
                command.ReservationId);

            // 4. Return success DTO
            return ReservationDtoMapping.ToSuccessResult(reservation);
        }
        catch (AggregateNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (InvalidAggregateStateException ex)
        {
            _logger.LogWarning(
                "Cannot confirm reservation {ReservationId} - invalid state transition. " +
                "Current state: {CurrentState}",
                command.ReservationId,
                ex.CurrentState);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogError(
                ex,
                "Domain error when confirming reservation {ReservationId}. Error code: {ErrorCode}",
                command.ReservationId,
                ex.ErrorCode);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while confirming reservation {ReservationId}",
                command.ReservationId);

            return ReservationDtoMapping.ToErrorResult(
                "An unexpected error occurred while confirming the reservation.");
        }
    }
}
