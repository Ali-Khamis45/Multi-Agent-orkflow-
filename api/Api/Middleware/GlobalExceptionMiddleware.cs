using AiAgentsTeam.Application.Common.Exceptions;

namespace AiAgentsTeam.Api.Middleware;

/// <summary>
/// Maps the handful of exception types Application handlers actually throw for
/// expected failure cases — a missing aggregate (<see cref="KeyNotFoundException"/>),
/// bad credentials (<see cref="UnauthorizedAccessException"/>), a state conflict
/// (<see cref="ConflictException"/>) — to the HTTP status they mean, instead of
/// letting them surface as a bare 500 (Code Review §1, Roadmap "Immediate"). Every
/// other, truly-unexpected exception is logged (with the request's CorrelationId
/// already in scope, since this sits inside CorrelationIdMiddleware) and returned as
/// a generic structured 500 — the client never sees a stack trace.
///
/// Registered before <see cref="ValidationExceptionMiddleware"/> so FluentValidation's
/// more specific 400 handling still takes precedence for that one case.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { title, status, detail });
    }
}
