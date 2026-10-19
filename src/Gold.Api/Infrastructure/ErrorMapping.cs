using Gold.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gold.Api.Infrastructure;

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> r, Func<T, IResult>? ok = null) =>
        r.IsSuccess ? (ok?.Invoke(r.Value!) ?? Results.Ok(r.Value)) : Problem(r.Error!);

    public static IResult Problem(Error e) => Results.Problem(
        statusCode: e.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        },
        title: e.Message,
        type: $"https://gold.example/errors/{e.Code}",
        extensions: new Dictionary<string, object?> { ["code"] = e.Code });
}

/// <summary>Global handler: ProblemDetails with traceId, never a stack trace.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IProblemDetailsService pds) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            Gold.Domain.Common.DomainException => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "اطلاعات هم‌زمان تغییر کرده است؛ دوباره تلاش کنید."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "درخواست نامعتبر است."),
            _ => (StatusCodes.Status500InternalServerError, "خطای داخلی سرور."),
        };
        if (status == 500) log.LogError(ex, "Unhandled exception");
        ctx.Response.StatusCode = status;
        return await pds.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        });
    }
}
