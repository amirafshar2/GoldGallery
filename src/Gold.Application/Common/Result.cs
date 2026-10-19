namespace Gold.Application.Common;

public enum ErrorKind { Validation = 1, NotFound = 2, Conflict = 3, Forbidden = 4, Unauthorized = 5, Unavailable = 6 }

/// <summary>Business error. Mapped to ProblemDetails by the API (422/404/409/403/401/503).</summary>
public sealed record Error(ErrorKind Kind, string Code, string Message)
{
    public static Error Validation(string code, string msg) => new(ErrorKind.Validation, code, msg);
    public static Error NotFound(string code, string msg) => new(ErrorKind.NotFound, code, msg);
    public static Error Conflict(string code, string msg) => new(ErrorKind.Conflict, code, msg);
    public static Error Forbidden(string code, string msg) => new(ErrorKind.Forbidden, code, msg);
    public static Error Unauthorized(string code, string msg) => new(ErrorKind.Unauthorized, code, msg);
    public static Error Unavailable(string code, string msg) => new(ErrorKind.Unavailable, code, msg);
}

public readonly record struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    private Result(T? value, Error? error) { Value = value; Error = error; }

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}

public readonly record struct Unit
{
    public static Unit Value => default;
}
