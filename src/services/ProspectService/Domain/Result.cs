namespace ProspectService.Domain;

/// <summary>
/// Result pattern for handling success/failure without exceptions.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result<T> Success(T value)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Value = value
        };
    }

    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = errors
        };
    }

    public static Result<T> Failure(string error)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error }
        };
    }
}
