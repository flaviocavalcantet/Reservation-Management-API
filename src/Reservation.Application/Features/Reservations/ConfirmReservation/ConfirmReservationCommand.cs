using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;

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

    public ConfirmReservationHandler(
        IReservationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationOperationResultDto> Handle(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Load the aggregate from repository
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);
            
            if (reservation is null)
            {
                return ReservationDtoMapping.ToErrorResult(
                    $"Reservation with ID {command.ReservationId} not found.");
            }

            // 2. Apply business operation (enforces transition rules in domain)
            //    If status transition is invalid, domain throws InvalidOperationException
            reservation.Confirm();

            // 3. Persist changes
            await _repository.UpdateAsync(reservation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Return success DTO with updated status
            return ReservationDtoMapping.ToSuccessResult(reservation);
        }
        catch (InvalidOperationException ex)
        {
            // Domain business rule violation
            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            return ReservationDtoMapping.ToErrorResult(
                $"An error occurred while confirming the reservation: {ex.Message}");
        }
    }
}
