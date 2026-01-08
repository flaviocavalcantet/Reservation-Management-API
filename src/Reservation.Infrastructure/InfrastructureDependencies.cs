using Microsoft.Extensions.DependencyInjection;
using Reservation.Application.Abstractions;
using Reservation.Domain.Abstractions;
using Reservation.Infrastructure.Persistence;

namespace Reservation.Infrastructure;

/// <summary>
/// Extension methods for configuring the Infrastructure layer dependencies.
/// Follows the SOLID principle of Dependency Inversion by providing abstractions.
/// Called from the API layer's DI configuration to keep infrastructure concerns separated.
/// </summary>
public static class InfrastructureDependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Register DbContext with PostgreSQL connection
        // Will be configured in the API layer with AddDbContext<ReservationDbContext>
        
        // Register repositories
        // Example: services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));

        // Register Unit of Work
        // services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ReservationDbContext>());

        // Register Domain Event Publisher
        // services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }
}
