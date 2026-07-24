using System.Text.Json;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Workflows.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiAgentsTeam.Infrastructure.EventBus;

/// <summary>
/// Reacts to events published by the AI Runtime — <c>TaskCompleted</c>/<c>TaskFailed</c>
/// — by Sending the corresponding Application command, which updates domain state and
/// re-triggers the Scheduler (ARCHITECTURE.md §5.2 step 4, §10 Self-Healing). This is
/// the concrete "agents communicate only through events" boundary (§6): the AI Runtime
/// never calls into .NET directly to mutate workflow state.
/// </summary>
public sealed class OrchestratorEventConsumer(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<OrchestratorEventConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        eventBus.SubscribeAsync("orchestrator", Environment.MachineName, HandleAsync, stoppingToken);

    private async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        switch (envelope.Type)
        {
            case EventTypes.TaskCompleted:
            {
                var payload = JsonSerializer.Deserialize<TaskCompletedPayload>(envelope.PayloadJson)!;
                await mediator.Send(new CompleteTaskCommand(
                    payload.TaskNodeId, payload.OutputsJson, payload.Confidence, payload.RiskLevel, payload.ReasoningSummary),
                    cancellationToken);
                break;
            }
            case EventTypes.TaskFailed:
            {
                var payload = JsonSerializer.Deserialize<TaskFailedPayload>(envelope.PayloadJson)!;
                await mediator.Send(new FailTaskCommand(payload.TaskNodeId, payload.ReasonJson), cancellationToken);
                break;
            }
            default:
                // Every other event type is informational (dashboards, audit trail) —
                // this consumer only reacts to the two that drive scheduling forward.
                logger.LogDebug("Ignoring event {Type} ({EventId})", envelope.Type, envelope.EventId);
                break;
        }
    }

    private sealed record TaskCompletedPayload(Guid TaskNodeId, string OutputsJson, double Confidence, string RiskLevel, string? ReasoningSummary);
    private sealed record TaskFailedPayload(Guid TaskNodeId, string? ReasonJson);
}
