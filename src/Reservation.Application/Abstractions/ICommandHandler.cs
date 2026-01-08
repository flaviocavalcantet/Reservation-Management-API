using MediatR;

namespace Reservation.Application.Abstractions;

/// <summary>
/// Command handler interface for write operations. Commands represent intentions to modify state.
/// Enables validation, authorization, and side-effect management through the CQRS pattern.
/// </summary>
/// <typeparam name="TCommand">The command request type</typeparam>
/// <typeparam name="TResponse">The command response type</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Base marker interface for commands
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
