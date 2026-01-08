using Microsoft.Extensions.DependencyInjection;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Reservation.Infrastructure.Persistence;
using Reservation.Infrastructure.Repositories;

namespace Reservation.Infrastructure;

/// <summary>
/// Extension methods for configuring the Infrastructure layer dependencies.
/// Follows the SOLID principle of Dependency Inversion by providing abstractions.
/// Called from the API layer's DI configuration to keep infrastructure concerns separated.
/// </summary>
public static class InfrastructureDependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register specialized repositories
        // IReservationRepository implementation with specialized queries
        services.AddScoped<IReservationRepository, ReservationRepository>();
        
        // Generic repository for any aggregate that needs standard CRUD
        // Can be used for other aggregates in the future
        services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));

        // Unit of Work is implemented by ReservationDbContext
        // The DbContext is already registered in Program.cs via AddDbContext<ReservationDbContext>
        // Here we just expose IUnitOfWork interface to the service provider
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReservationDbContext>());

        // TODO: Domain Event Publisher
        // services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }
}

