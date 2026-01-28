/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Implements the Result pattern to avoid throwing exceptions for expected failures.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T> 
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; }
    private Result(bool success, T? value, string error) 
        => (IsSuccess, Value, Error) = (success, value, error);
    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}