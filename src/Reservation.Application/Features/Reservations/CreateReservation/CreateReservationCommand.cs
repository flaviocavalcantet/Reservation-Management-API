using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Microsoft.Extensions.Logging;

namespace Reservation.Application.Features.Reservations.CreateReservation;

/// <summary>
/// Command to create a new reservation.
/// 
/// Commands in CQRS:
/// - Represent intentions to change state
/// - Have side effects (modify database)
/// - Return a result
/// - Are validated before handling
/// </summary>
public record CreateReservationCommand(
    /// <summary>Customer making the reservation</summary>
    Guid CustomerId,
    
    /// <summary>When the reservation starts</summary>
    DateTime StartDate,
    
    /// <summary>When the reservation ends</summary>
    DateTime EndDate
) : ICommand<ReservationOperationResultDto>;

/// <summary>
/// Handles CreateReservationCommand.
/// 
/// Responsibilities:
/// 1. Validate input (via pipeline behaviors)
/// 2. Create domain aggregate (enforces business rules)
/// 3. Persist to repository
/// 4. Publish domain events (handled by infrastructure)
/// 5. Return result DTO
/// </summary>
public class CreateReservationHandler : ICommandHandler<CreateReservationCommand, ReservationOperationResultDto>
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReservationHandler> _logger;

    public CreateReservationHandler(
        IReservationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateReservationHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationOperationResultDto> Handle(
        CreateReservationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating reservation for customer {CustomerId} from {StartDate} to {EndDate}",
            command.CustomerId,
            command.StartDate,
            command.EndDate);

        try
        {
            // 1. Create domain aggregate - this enforces business rules
            //    If dates are invalid, an exception is thrown by the domain
            var reservation = Domain.Reservations.Reservation.Create(
                customerId: command.CustomerId,
                startDate: command.StartDate,
                endDate: command.EndDate);

            _logger.LogDebug(
                "Reservation entity created with ID {ReservationId} in {Status} status",
                reservation.Id,
                reservation.Status);

            // 2. Persist the aggregate
            //    The repository saves the entity to the database
            await _repository.AddAsync(reservation, cancellationToken);
            
            // 3. Commit transaction
            //    Unit of Work ensures atomic persistence
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} created and persisted successfully for customer {CustomerId}",
                reservation.Id,
                command.CustomerId);

            // 4. Return success DTO
            //    Domain events will be published by infrastructure layer after save
            return ReservationDtoMapping.ToSuccessResult(reservation);
        }
        catch (InvalidOperationException ex)
        {
            // Domain validation failed - return error to client
            _logger.LogWarning(
                ex,
                "Domain validation failed for reservation creation. CustomerId: {CustomerId}. Error: {ErrorMessage}",
                command.CustomerId,
                ex.Message);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected error - log and return generic error message
            var innerMessage = ex.InnerException?.Message ?? "";
            _logger.LogError(
                ex,
                "Unexpected error occurred while creating reservation for customer {CustomerId}. Inner error: {InnerError}",
                command.CustomerId,
                innerMessage);

            return ReservationDtoMapping.ToErrorResult(
                $"An error occurred while creating the reservation: {ex.Message} {(string.IsNullOrEmpty(innerMessage) ? "" : $"Details: {innerMessage}")}");
        }
    }
}
