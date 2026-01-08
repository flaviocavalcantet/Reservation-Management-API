namespace Reservation.Domain.Abstractions;

/// <summary>
/// Base class for value objects in DDD. Value Objects are immutable objects defined by their attributes,
/// not by their identity. They are compared by value, not by reference.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the atomic values that compose this value object.
    /// Used for value equality comparison.
    /// </summary>
    protected abstract IEnumerable<object> GetAtomicValues();

    /// <summary>
    /// Value objects are equal if all their atomic values are equal
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var valueObject = (ValueObject)obj;
        return GetAtomicValues().SequenceEqual(valueObject.GetAtomicValues());
    }

    /// <summary>
    /// Hash code is based on atomic values to maintain consistency with Equals
    /// </summary>
    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Aggregate(default(HashCode), (hashCode, value) =>
            {
                hashCode.Add(value);
                return hashCode;
            })
            .ToHashCode();
    }

    /// <summary>
    /// Overload equality operator for convenience
    /// </summary>
    public static bool operator ==(ValueObject left, ValueObject right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Overload inequality operator for convenience
    /// </summary>
    public static bool operator !=(ValueObject left, ValueObject right)
    {
        return !Equals(left, right);
    }
}
