using FluentValidation;

namespace AiAgentsTeam.Api.Middleware;

/// <summary>
/// Translates a FluentValidation <see cref="ValidationException"/> (raised by
/// ValidationBehavior, Phase 1.5 §10) into a 400 Bad Request with a structured
/// error body, instead of an unhandled 500. Every other exception passes through
/// unchanged — this middleware only narrows one specific, expected case.
/// </summary>
public sealed class ValidationExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "One or more validation errors occurred.",
                status = 400,
                errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }
    }
}
