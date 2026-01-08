namespace Reservation.Domain.Abstractions;

/// <summary>
/// Base class for all domain entities. Entities have identity and are mutable.
/// They are compared by their unique identifier (Id), not by their attributes.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for this entity
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Timestamp when entity was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public DateTime? ModifiedAt { get; protected set; }

    /// <summary>
    /// Domain events that occurred on this entity. Cleared after being published.
    /// </summary>
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Returns a read-only collection of domain events
    /// </summary>
    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events after they've been published
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Entities are equal if they have the same identity
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var entity = (Entity)obj;
        return entity.Id == Id;
    }

    /// <summary>
    /// Hash code is based on identity
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Overload equality operator for convenience
    /// </summary>
    public static bool operator ==(Entity left, Entity right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Overload inequality operator for convenience
    /// </summary>
    public static bool operator !=(Entity left, Entity right)
    {
        return !Equals(left, right);
    }
}
