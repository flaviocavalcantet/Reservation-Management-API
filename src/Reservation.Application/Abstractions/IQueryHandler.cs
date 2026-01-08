using MediatR;

namespace Reservation.Application.Abstractions;

/// <summary>
/// Query handler interface for read operations. Queries are requests that don't modify state.
/// Enables separation of reads from writes (CQRS pattern) for better scalability.
/// </summary>
/// <typeparam name="TQuery">The query request type</typeparam>
/// <typeparam name="TResponse">The query response type</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Base marker interface for queries
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
