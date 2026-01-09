using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Reservations;
using Microsoft.Extensions.Logging;

namespace Reservation.Application.Features.Reservations.GetReservations;

/// <summary>
/// Query to retrieve reservations for a customer.
/// 
/// Queries in CQRS:
/// - Represent intentions to read state
/// - Have NO side effects
/// - Return data
/// - Are not validated (caller must provide valid CustomerId)
/// </summary>
public record GetReservationsQuery(
    /// <summary>Customer ID to retrieve reservations for</summary>
    Guid CustomerId
) : IQuery<IEnumerable<ReservationDto>>;

/// <summary>
/// Handles GetReservationsQuery.
/// 
/// Responsibilities:
/// 1. Query repository for customer's reservations
/// 2. Map domain entities to DTOs
/// 3. Return result collection
/// 
/// This is a read-only operation with no business logic enforcement.
/// </summary>
public class GetReservationsHandler : IQueryHandler<GetReservationsQuery, IEnumerable<ReservationDto>>
{
    private readonly IReservationRepository _repository;
    private readonly ILogger<GetReservationsHandler> _logger;

    public GetReservationsHandler(
        IReservationRepository repository,
        ILogger<GetReservationsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ReservationDto>> Handle(
        GetReservationsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving reservations for customer {CustomerId}",
            query.CustomerId);

        try
        {
            // Query the repository for this customer's reservations
            var reservations = await _repository.GetByCustomerIdAsync(
                query.CustomerId,
                cancellationToken);

            var reservationList = reservations.ToList();

            _logger.LogInformation(
                "Retrieved {Count} reservations for customer {CustomerId}",
                reservationList.Count,
                query.CustomerId);

            _logger.LogDebug(
                "Reservation statuses for customer {CustomerId}: {Statuses}",
                query.CustomerId,
                string.Join(", ", reservationList.Select(r => r.Status.Value)));

            // Map domain entities to DTOs for API response
            return reservationList
                .Select(r => ReservationDtoMapping.ToDto(r))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving reservations for customer {CustomerId}",
                query.CustomerId);

            throw;
        }
    }
}
