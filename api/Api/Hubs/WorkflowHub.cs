using Microsoft.AspNetCore.SignalR;

namespace AiAgentsTeam.Api.Hubs;

/// <summary>
/// Real-time push channel for the frontend DAG viewer (ARCHITECTURE.md §23).
/// Clients join a group per WorkflowRun to receive only that run's updates.
/// </summary>
public sealed class WorkflowHub : Hub
{
    public Task JoinWorkflow(string workflowRunId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(workflowRunId));

    public Task LeaveWorkflow(string workflowRunId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(workflowRunId));

    public static string GroupName(string workflowRunId) => $"workflow:{workflowRunId}";
}
