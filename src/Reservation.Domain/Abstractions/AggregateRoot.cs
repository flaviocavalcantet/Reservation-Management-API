namespace Reservation.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots in DDD. An aggregate root is the topmost entity in an aggregate cluster.
/// It ensures business rule enforcement and consistency within the aggregate boundary.
/// All external references should point to the aggregate root, not to other entities within the aggregate.
/// </summary>
public abstract class AggregateRoot : Entity
{
    // Aggregate root extends Entity with domain-driven semantics.
    // The domain event collection inherited from Entity serves as the aggregate's
    // event journal, enabling event-based consistency patterns.
}
