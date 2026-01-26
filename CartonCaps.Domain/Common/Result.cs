public class Result<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }

    protected Result(bool success, T? value, string error) 
        => (IsSuccess, Value, Error) = (success, value, error);

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}