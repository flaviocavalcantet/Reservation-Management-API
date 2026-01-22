namespace Reservation.Application.Common;

/// <summary>
/// Generic result wrapper for application operations.
/// Provides a standardized response pattern across all handlers.
/// 
/// Pattern: Result pattern with single responsibility
/// - Encapsulates success/failure state
/// - Carries error messages or data
/// - Type-safe return values
/// - Reduces null checking throughout codebase
/// </summary>
/// <typeparam name="T">The type of data returned on success</typeparam>
public abstract record Result<T>
{
    /// <summary>
    /// Indicates whether the operation succeeded
    /// </summary>
    public virtual bool IsSuccess { get; init; }

    /// <summary>
    /// Creates a success result with data
    /// </summary>
    public static Result<T> Success(T data) => new SuccessResult(data);

    /// <summary>
    /// Creates a failure result with error message
    /// </summary>
    public static Result<T> Failure(string error) => new FailureResult(error);

    /// <summary>
    /// Success result variant
    /// </summary>
    public record SuccessResult(T Data) : Result<T>
    {
        /// <summary>
        /// Always true for success results
        /// </summary>
        public override bool IsSuccess { get; init; } = true;
    }

    /// <summary>
    /// Failure result variant
    /// </summary>
    public record FailureResult(string Error) : Result<T>
    {
        /// <summary>
        /// Always false for failure results
        /// </summary>
        public override bool IsSuccess { get; init; } = false;
    }

    /// <summary>
    /// Applies a mapping function if the result is successful
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return this switch
        {
            SuccessResult success => onSuccess(success.Data),
            FailureResult failure => onFailure(failure.Error),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    /// <summary>
    /// Applies an action based on the result state
    /// </summary>
    public void Match(
        Action<T> onSuccess,
        Action<string> onFailure)
    {
        switch (this)
        {
            case SuccessResult success:
                onSuccess(success.Data);
                break;
            case FailureResult failure:
                onFailure(failure.Error);
                break;
        }
    }
}

/// <summary>
/// Result wrapper for operations that don't return data
/// </summary>
public abstract record UnitResult
{
    /// <summary>
    /// Indicates whether the operation succeeded
    /// </summary>
    public virtual bool IsSuccess { get; init; }

    /// <summary>
    /// Creates a success result
    /// </summary>
    public static UnitResult Success() => new SuccessUnitResult();

    /// <summary>
    /// Creates a failure result with error message
    /// </summary>
    public static UnitResult Failure(string error) => new FailureUnitResult(error);

    /// <summary>
    /// Success result variant
    /// </summary>
    public record SuccessUnitResult : UnitResult
    {
        /// <summary>
        /// Always true for success results
        /// </summary>
        public override bool IsSuccess { get; init; } = true;
    }

    /// <summary>
    /// Failure result variant
    /// </summary>
    public record FailureUnitResult(string Error) : UnitResult
    {
        /// <summary>
        /// Always false for failure results
        /// </summary>
        public override bool IsSuccess { get; init; } = false;
    }

    /// <summary>
    /// Applies an action based on the result state
    /// </summary>
    public void Match(
        Action onSuccess,
        Action<string> onFailure)
    {
        switch (this)
        {
            case SuccessUnitResult:
                onSuccess();
                break;
            case FailureUnitResult failure:
                onFailure(failure.Error);
                break;
        }
    }
}
