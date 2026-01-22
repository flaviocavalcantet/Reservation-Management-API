namespace Reservation.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors. All business rule violations
/// should throw exceptions derived from this class to ensure consistent
/// exception handling across the application layers.
/// 
/// Pattern: Exception hierarchy with semantic meaning
/// - Enables layer-specific error handling
/// - Supports typed exception catching in handlers
/// - Provides clear error context for logging
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DomainException class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected DomainException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the error code for this domain exception.
    /// Used for categorizing and logging domain errors.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    /// Gets the severity level of this exception.
    /// Used to determine appropriate logging level.
    /// </summary>
    public virtual ExceptionSeverity Severity => ExceptionSeverity.Error;
}

/// <summary>
/// Represents a business rule violation in the domain.
/// Thrown when domain invariants are violated.
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    /// <summary>
    /// The name of the business rule that was violated
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Initializes a new instance of BusinessRuleViolationException.
    /// </summary>
    /// <param name="ruleName">The name of the violated business rule</param>
    /// <param name="message">Description of why the rule was violated</param>
    public BusinessRuleViolationException(string ruleName, string message)
        : base($"Business rule violation [{ruleName}]: {message}")
    {
        RuleName = ruleName;
    }

    /// <summary>
    /// The error code for business rule violations
    /// </summary>
    public override string ErrorCode => "BR_VIOLATION";

    /// <summary>
    /// Business rule violations are expected client errors
    /// </summary>
    public override ExceptionSeverity Severity => ExceptionSeverity.Warning;
}

/// <summary>
/// Represents an invalid domain state operation.
/// Thrown when an operation is attempted on an aggregate in an invalid state.
/// </summary>
public class InvalidAggregateStateException : DomainException
{
    /// <summary>
    /// The current state of the aggregate
    /// </summary>
    public string CurrentState { get; }

    /// <summary>
    /// The requested operation
    /// </summary>
    public string RequestedOperation { get; }

    /// <summary>
    /// Initializes a new instance of InvalidAggregateStateException.
    /// </summary>
    /// <param name="currentState">Current state of the aggregate</param>
    /// <param name="requestedOperation">The operation attempted</param>
    /// <param name="message">Explanation of why the operation is invalid</param>
    public InvalidAggregateStateException(string currentState, string requestedOperation, string message)
        : base($"Cannot perform '{requestedOperation}' when aggregate is in '{currentState}' state. {message}")
    {
        CurrentState = currentState;
        RequestedOperation = requestedOperation;
    }

    /// <summary>
    /// The error code for invalid aggregate state
    /// </summary>
    public override string ErrorCode => "INVALID_STATE";

    /// <summary>
    /// Invalid state transitions are expected client errors
    /// </summary>
    public override ExceptionSeverity Severity => ExceptionSeverity.Warning;
}

/// <summary>
/// Represents a validation error for domain values.
/// Thrown when domain data doesn't meet validation criteria.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// The property or field that failed validation
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Validation error details
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Initializes a new instance of DomainValidationException.
    /// </summary>
    /// <param name="propertyName">Name of the property that failed validation</param>
    /// <param name="errors">Array of error messages</param>
    public DomainValidationException(string propertyName, params string[] errors)
        : base($"Domain validation failed for '{propertyName}': {string.Join("; ", errors)}")
    {
        PropertyName = propertyName;
        Errors = errors;
    }

    public override string ErrorCode => "VALIDATION_FAILED";
    public override ExceptionSeverity Severity => ExceptionSeverity.Warning;
}

/// <summary>
/// Represents a conflict when creating or updating an aggregate.
/// Thrown when a resource already exists or constraint is violated.
/// </summary>
public class AggregateConflictException : DomainException
{
    /// <summary>
    /// The ID or identifier of the conflicting aggregate
    /// </summary>
    public string ConflictingIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of AggregateConflictException.
    /// </summary>
    /// <param name="aggregateType">Type of aggregate</param>
    /// <param name="conflictingIdentifier">Identifier of the conflicting resource</param>
    /// <param name="message">Description of the conflict</param>
    public AggregateConflictException(string aggregateType, string conflictingIdentifier, string message)
        : base($"Conflict with {aggregateType} '{conflictingIdentifier}': {message}")
    {
        ConflictingIdentifier = conflictingIdentifier;
    }

    public override string ErrorCode => "AGGREGATE_CONFLICT";
}

/// <summary>
/// Represents a not found condition for domain resources.
/// Thrown when trying to access a resource that doesn't exist.
/// </summary>
public class AggregateNotFoundException : DomainException
{
    /// <summary>
    /// The type of aggregate that was not found
    /// </summary>
    public string AggregateType { get; }

    /// <summary>
    /// The ID of the aggregate that was not found
    /// </summary>
    public object AggregateId { get; }

    /// <summary>
    /// Initializes a new instance of AggregateNotFoundException.
    /// </summary>
    /// <param name="aggregateType">Type of aggregate</param>
    /// <param name="aggregateId">ID of the aggregate</param>
    public AggregateNotFoundException(string aggregateType, object aggregateId)
        : base($"{aggregateType} with ID '{aggregateId}' was not found.")
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
    }

    public override string ErrorCode => "NOT_FOUND";
    public override ExceptionSeverity Severity => ExceptionSeverity.Warning;
}

/// <summary>
/// Severity levels for domain exceptions.
/// Used to determine appropriate logging and response levels.
/// </summary>
public enum ExceptionSeverity
{
    /// <summary>
    /// Warning level - expected business rule violation (client error)
    /// </summary>
    Warning = 0,

    /// <summary>
    /// Error level - unexpected error in business logic
    /// </summary>
    Error = 1,

    /// <summary>
    /// Critical level - system-level failure
    /// </summary>
    Critical = 2
}
