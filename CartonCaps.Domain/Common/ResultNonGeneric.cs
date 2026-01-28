namespace CartonCaps.Domain.Common;

/// <summary>
/// Represents a non-generic result for operations that don't return a value.
/// Use this for void operations that can succeed or fail.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed, otherwise empty string.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Initializes a new instance of the Result class.
    /// </summary>
    protected Result(bool success, string error)
        => (IsSuccess, Error) = (success, error);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(string error) => new(false, error);
}
