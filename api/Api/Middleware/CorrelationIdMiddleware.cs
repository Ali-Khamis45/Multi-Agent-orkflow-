namespace AiAgentsTeam.Api.Middleware;

/// <summary>
/// Per-HTTP-request correlation (Phase 1.5 §2), complementary to the per-workflow
/// CorrelationId threaded through WorkflowRun/TaskNode/Artifact/ReasoningTrace/etc.
/// Every request either carries an <c>X-Correlation-Id</c> header (the AI Runtime
/// sends the workflow's CorrelationId here) or gets one generated; it is echoed
/// back on the response and pushed into the logging scope so every log line for
/// this request — including ones with no workflow context at all, e.g. a bare
/// registry call — can be grepped out by it.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
