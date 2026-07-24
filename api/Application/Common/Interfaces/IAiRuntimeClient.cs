namespace AiAgentsTeam.Application.Common.Interfaces;

/// <summary>
/// The one deliberate, server-to-server exception to "the AI Runtime is only
/// reached through events" (§6): kicking off a brand-new execution and
/// reading the (file-based) Prompt Registry are both requests *to* the AI
/// Runtime rather than facts already persisted in Postgres, so there is no
/// event/table to read instead. The dashboard (Phase 1.6) never calls the AI
/// Runtime directly — it calls the ASP.NET API, which calls this.
/// </summary>
public interface IAiRuntimeClient
{
    Task<Guid> SubmitIntakeAsync(string rawInput, Guid? workspaceId, CancellationToken cancellationToken);

    Task<string> GetPromptsJsonAsync(CancellationToken cancellationToken);
}
