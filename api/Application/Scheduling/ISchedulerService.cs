namespace AiAgentsTeam.Application.Scheduling;

/// <summary>
/// The DAG Scheduler (ARCHITECTURE.md §5.2) — purely mechanical: compute the ready
/// set, resolve a candidate agent per node from the Registry, dispatch, recompute on
/// completion. It has no opinion about workflow shape or agent identity; that
/// intelligence lives one layer up, in the Supervisor (E1).
/// </summary>
public interface ISchedulerService
{
    Task ScheduleReadyNodesAsync(Guid workflowRunId, CancellationToken cancellationToken = default);
}
