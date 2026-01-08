using MediatR;
using Reservation.API.DTOs;
using Reservation.Application.Features.Reservations.CancelReservation;
using Reservation.Application.Features.Reservations.ConfirmReservation;
using Reservation.Application.Features.Reservations.CreateReservation;
using Reservation.Application.Features.Reservations.GetReservations;

namespace Reservation.API.Endpoints;

/// <summary>
/// REST API endpoints for reservation management operations.
/// 
/// Implements the Vertical Slice Architecture pattern where each feature
/// (Create, Confirm, Cancel, Get) has its own endpoint implementation.
/// 
/// Pattern: POST /api/v1/reservations (create)
///         POST /api/v1/reservations/{id}/confirm (confirm)
///         POST /api/v1/reservations/{id}/cancel (cancel)
///         GET  /api/v1/reservations (list)
/// </summary>
public class ReservationEndpoints : EndpointGroup
{
    public override void Map(WebApplication app)
    {
        // Create a route group for API v1 reservations endpoints
        var group = app.MapGroup("/api/v1/reservations")
            .WithName("Reservations")
            .WithOpenApi()
            .WithTags("Reservations");

        // Endpoints
        group.MapCreateReservation();
        group.MapConfirmReservation();
        group.MapCancelReservation();
        group.MapGetReservations();
    }
}

/// <summary>
/// Extension methods for mapping reservation endpoints.
/// Organized for readability and maintainability.
/// </summary>
file static class ReservationEndpointExtensions
{
    /// <summary>
    /// POST /api/v1/reservations
    /// 
    /// Creates a new reservation with the provided details.
    /// 
    /// Request Body: CreateReservationRequest
    /// - CustomerId (GUID): Customer making the reservation
    /// - StartDate (DateTime): When the reservation begins
    /// - EndDate (DateTime): When the reservation ends
    /// 
    /// Returns: 201 Created with ReservationOperationResultDto
    /// Error: 400 Bad Request if validation fails, 500 Internal Server Error if processing fails
    /// </summary>
    internal static void MapCreateReservation(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateReservationAsync)
            .WithName("CreateReservation")
            .WithSummary("Create a new reservation")
            .WithDescription("Creates a new reservation for a customer. Validates dates and enforces business rules.")
            .Produces<global::Reservation.Application.DTOs.ReservationOperationResultDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateReservationAsync(
        CreateReservationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateReservationCommand(
            CustomerId: request.CustomerId,
            StartDate: request.StartDate,
            EndDate: request.EndDate);

        var result = await mediator.Send(command, cancellationToken);

        // Return 201 Created if successful, 400 Bad Request if failed
        return result.Success
            ? Results.Created($"/api/v1/reservations/{result.ReservationId}", result)
            : Results.BadRequest(result);
    }

    /// <summary>
    /// POST /api/v1/reservations/{id}/confirm
    /// 
    /// Confirms an existing reservation, transitioning it from Created to Confirmed status.
    /// 
    /// Route Parameters:
    /// - id (GUID): Reservation ID to confirm
    /// 
    /// Returns: 200 OK with ReservationOperationResultDto
    /// Error: 404 Not Found if reservation doesn't exist, 400 Bad Request if business rule violated
    /// </summary>
    internal static void MapConfirmReservation(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{id}/confirm", ConfirmReservationAsync)
            .WithName("ConfirmReservation")
            .WithSummary("Confirm a reservation")
            .WithDescription("Transitions a reservation from Created to Confirmed status. Only Created reservations can be confirmed.")
            .Produces<global::Reservation.Application.DTOs.ReservationOperationResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ConfirmReservationAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmReservationCommand(ReservationId: id);
        var result = await mediator.Send(command, cancellationToken);

        // Return 200 OK if successful, 400 Bad Request if business rule violated
        if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
            return Results.NotFound(result);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    /// <summary>
    /// POST /api/v1/reservations/{id}/cancel
    /// 
    /// Cancels an existing reservation.
    /// 
    /// Route Parameters:
    /// - id (GUID): Reservation ID to cancel
    /// 
    /// Request Body: CancelReservationRequest
    /// - Reason (string, optional): Reason for cancellation
    /// 
    /// Returns: 200 OK with ReservationOperationResultDto
    /// Error: 404 Not Found if reservation doesn't exist, 400 Bad Request if business rule violated
    /// 
    /// Business Rules:
    /// - Created reservations can be cancelled anytime
    /// - Confirmed reservations can only be cancelled before their start date
    /// - Already cancelled reservations cannot be cancelled again
    /// </summary>
    internal static void MapCancelReservation(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{id}/cancel", CancelReservationAsync)
            .WithName("CancelReservation")
            .WithSummary("Cancel a reservation")
            .WithDescription("Cancels a reservation. Business rules prevent cancelling confirmed reservations after their start date.")
            .Produces<global::Reservation.Application.DTOs.ReservationOperationResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CancelReservationAsync(
        Guid id,
        CancelReservationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CancelReservationCommand(
            ReservationId: id,
            Reason: request.Reason ?? "No reason provided");

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
            return Results.NotFound(result);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    /// <summary>
    /// GET /api/v1/reservations?customerId={customerId}
    /// 
    /// Retrieves all reservations for a specific customer.
    /// 
    /// Query Parameters:
    /// - customerId (GUID): Customer ID to retrieve reservations for
    /// 
    /// Returns: 200 OK with list of ReservationDto objects
    /// Error: 400 Bad Request if customerId is invalid, 500 Internal Server Error if processing fails
    /// </summary>
    internal static void MapGetReservations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetReservationsAsync)
            .WithName("GetReservations")
            .WithSummary("Get reservations for a customer")
            .WithDescription("Retrieves all reservations (created, confirmed, and cancelled) for a specific customer, ordered by start date descending.")
            .Produces<IEnumerable<global::Reservation.Application.DTOs.ReservationDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetReservationsAsync(
        Guid customerId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty)
            return Results.BadRequest(new { error = "CustomerId must be a valid GUID, not empty GUID" });

        var query = new GetReservationsQuery(CustomerId: customerId);
        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(result);
    }
}
