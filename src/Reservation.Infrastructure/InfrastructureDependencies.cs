using Microsoft.Extensions.DependencyInjection;
using Reservation.Application.Repositories;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Reservation.Infrastructure.Identity;
using Reservation.Infrastructure.Persistence;
using Reservation.Infrastructure.Repositories;

namespace Reservation.Infrastructure;

/// <summary>
/// Extension methods for configuring the Infrastructure layer dependencies.
/// Follows the SOLID principle of Dependency Inversion by providing abstractions.
/// Called from the API layer's DI configuration to keep infrastructure concerns separated.
/// 
/// Registers:
/// - Data access repositories (IReservationRepository, IRepository{T})
/// - Identity user repository (IIdentityUserRepository)
/// - Unit of Work pattern (IUnitOfWork)
/// </summary>
public static class InfrastructureDependencies
{
    /// <summary>
    /// Adds all Infrastructure layer services to the dependency injection container.
    /// 
    /// This is called from API layer's Program.cs:
    /// <code>
    /// services.AddInfrastructure();
    /// </code>
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register specialized repositories
        // IReservationRepository implementation with specialized queries
        services.AddScoped<IReservationRepository, ReservationRepository>();
        
        // Generic repository for any aggregate that needs standard CRUD
        // Can be used for other aggregates in the future
        services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));

        // Identity user repository for authentication/authorization operations
        // Implements IIdentityUserRepository (Application abstraction)
        // Wraps ASP.NET Core Identity's UserManager
        services.AddScoped<IIdentityUserRepository, IdentityUserRepository>();

        // Unit of Work is implemented by ReservationDbContext
        // The DbContext is already registered in Program.cs via AddDbContext<ReservationDbContext>
        // Here we just expose IUnitOfWork interface to the service provider
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReservationDbContext>());

        // TODO: Domain Event Publisher
        // services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }
}

