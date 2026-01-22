using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Exceptions;
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
            //    If dates are invalid, a domain exception is thrown
            var reservation = Domain.Reservations.Reservation.Create(
                customerId: command.CustomerId,
                startDate: command.StartDate,
                endDate: command.EndDate);

            _logger.LogDebug(
                "Reservation entity created with ID {ReservationId} in {Status} status",
                reservation.Id,
                reservation.Status);

            // 2. Persist the aggregate
            await _repository.AddAsync(reservation, cancellationToken);
            
            // 3. Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reservation {ReservationId} created successfully for customer {CustomerId}",
                reservation.Id,
                command.CustomerId);

            // 4. Return success DTO
            return ReservationDtoMapping.ToSuccessResult(reservation);
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed while creating reservation for customer {CustomerId}. " +
                "Property: {PropertyName}. Errors: {Errors}",
                command.CustomerId,
                ex.PropertyName,
                string.Join("; ", ex.Errors));

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (BusinessRuleViolationException ex)
        {
            _logger.LogWarning(
                "Business rule violation when creating reservation for customer {CustomerId}. " +
                "Rule: {RuleName}. Message: {Message}",
                command.CustomerId,
                ex.RuleName,
                ex.Message);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogError(
                ex,
                "Domain error while creating reservation for customer {CustomerId}. " +
                "Error code: {ErrorCode}. Message: {Message}",
                command.CustomerId,
                ex.ErrorCode,
                ex.Message);

            return ReservationDtoMapping.ToErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while creating reservation for customer {CustomerId}",
                command.CustomerId);

            return ReservationDtoMapping.ToErrorResult(
                "An unexpected error occurred while creating the reservation.");
        }
    }
}
