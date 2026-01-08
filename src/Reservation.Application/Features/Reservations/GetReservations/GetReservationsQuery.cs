using Reservation.Application.Abstractions;
using Reservation.Application.DTOs;
using Reservation.Domain.Reservations;

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

    public GetReservationsHandler(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationDto>> Handle(
        GetReservationsQuery query,
        CancellationToken cancellationToken)
    {
        // Query the repository for this customer's reservations
        var reservations = await _repository.GetByCustomerIdAsync(
            query.CustomerId,
            cancellationToken);

        // Map domain entities to DTOs for API response
        return reservations
            .Select(r => ReservationDtoMapping.ToDto(r))
            .ToList();
    }
}
