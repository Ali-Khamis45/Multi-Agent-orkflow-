using AiAgentsTeam.Api.Hubs;
using AiAgentsTeam.Application.Common.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace AiAgentsTeam.Api.EventRelay;

/// <summary>
/// Relays every bus event into the SignalR group for its WorkflowRun, so the DAG
/// viewer updates live (ARCHITECTURE.md §23). Runs as its own consumer group
/// ("signalr-relay") — Redis Streams fans the same event out to this group and to
/// the Infrastructure layer's <c>OrchestratorEventConsumer</c> ("orchestrator")
/// independently, so a slow/broken UI relay can never block scheduling.
/// </summary>
public sealed class SignalRRelayHostedService(IEventBus eventBus, IHubContext<WorkflowHub> hub) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        eventBus.SubscribeAsync("signalr-relay", Environment.MachineName, HandleAsync, stoppingToken);

    private async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (envelope.WorkflowRunId is not { } workflowRunId) return;

        await hub.Clients
            .Group(WorkflowHub.GroupName(workflowRunId.ToString()))
            .SendAsync("workflowEvent", new
            {
                envelope.Type,
                envelope.TaskId,
                envelope.ProducedBy,
                envelope.Timestamp,
                envelope.Confidence,
                envelope.RiskLevel
            }, cancellationToken);
    }
}
