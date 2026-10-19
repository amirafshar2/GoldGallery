using FluentValidation;

namespace Gold.Api.Infrastructure;

/// <summary>Runs the FluentValidation validator of the first argument of type T; returns 422 ValidationProblem.</summary>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var arg = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null) return Results.Problem(statusCode: 400, title: "بدنه‌ی درخواست خالی است.");
        var res = await validator.ValidateAsync(arg, ctx.HttpContext.RequestAborted);
        if (!res.IsValid)
            return Results.ValidationProblem(res.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity, title: "اطلاعات ورودی نامعتبر است.");
        return await next(ctx);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder Validate<T>(this RouteHandlerBuilder b) => b.AddEndpointFilter<ValidationFilter<T>>();
}
