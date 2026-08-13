namespace SmartX.Shared.Models;

public class Result<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Error { get; init; }

    public static Result<T> Ok(T data)
    {
        return new Result<T>
        {
            Success = true,
            Data = data
        };
    }

    public static Result<T> Fail(string error)
    {
        return new Result<T>
        {
            Success = false,
            Error = error
        };
    }
}