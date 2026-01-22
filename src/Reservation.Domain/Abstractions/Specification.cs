using System.Linq.Expressions;

namespace Reservation.Domain.Abstractions;

/// <summary>
/// Specification pattern base class for query composition.
/// Encapsulates query logic in a reusable, testable way.
/// 
/// Pattern: Specification pattern for complex queries
/// - Separates query logic from repository
/// - Enables query reuse and composition
/// - Improves testability of query logic
/// - Maintains repository simplicity
/// </summary>
/// <typeparam name="TAggregate">The aggregate type to query</typeparam>
public abstract class Specification<TAggregate> where TAggregate : AggregateRoot
{
    /// <summary>
    /// The criteria for filtering entities
    /// </summary>
    public Expression<Func<TAggregate, bool>> Criteria { get; protected set; } = _ => true;

    /// <summary>
    /// Include related data (lazy loading)
    /// </summary>
    public List<Expression<Func<TAggregate, object>>> Includes { get; } = new();

    /// <summary>
    /// String-based include for primitive collections
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Ordering specification
    /// </summary>
    public Expression<Func<TAggregate, object>>? OrderByExpression { get; protected set; }

    /// <summary>
    /// Indicates descending order
    /// </summary>
    public bool IsOrderByDescending { get; protected set; }

    /// <summary>
    /// Pagination: Skip count
    /// </summary>
    public int? Take { get; protected set; }

    /// <summary>
    /// Pagination: Take count
    /// </summary>
    public int? Skip { get; protected set; }

    /// <summary>
    /// Indicates whether pagination is enabled
    /// </summary>
    public bool IsPagingEnabled { get; protected set; }

    /// <summary>
    /// Adds an include expression for eager loading
    /// </summary>
    protected virtual void AddInclude(Expression<Func<TAggregate, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include for primitive collections
    /// </summary>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies ordering to the query
    /// </summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Applies ordering by specified property ascending
    /// </summary>
    protected virtual void ApplyOrderBy(Expression<Func<TAggregate, object>> orderByExpression)
    {
        OrderByExpression = orderByExpression;
        IsOrderByDescending = false;
    }

    /// <summary>
    /// Applies ordering by specified property descending
    /// </summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<TAggregate, object>> orderByExpression)
    {
        OrderByExpression = orderByExpression;
        IsOrderByDescending = true;
    }
}

/// <summary>
/// Specification pattern with count capabilities for pagination
/// </summary>
/// <typeparam name="TAggregate">The aggregate type to query</typeparam>
public abstract class SpecificationWithCount<TAggregate> : Specification<TAggregate>
    where TAggregate : AggregateRoot
{
    /// <summary>
    /// Separate criteria for counting (before pagination)
    /// </summary>
    public Expression<Func<TAggregate, bool>>? CountCriteria { get; protected set; }

    /// <summary>
    /// Sets the count criteria
    /// </summary>
    protected void SetCountCriteria(Expression<Func<TAggregate, bool>> countCriteria)
    {
        CountCriteria = countCriteria;
    }
}
