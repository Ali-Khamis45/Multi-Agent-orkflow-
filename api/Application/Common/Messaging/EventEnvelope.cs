namespace AiAgentsTeam.Application.Common.Messaging;

/// <summary>
/// The one shared contract between the .NET Orchestration Service and the Python
/// AI Runtime (ARCHITECTURE.md §6.2). Every event, from either runtime, is this
/// shape — it is the only coupling between the two processes.
/// </summary>
public sealed record EventEnvelope
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }
    public int Version { get; init; } = 1;
    public required Guid WorkspaceId { get; init; }
    public Guid? WorkflowRunId { get; init; }
    public Guid? TaskId { get; init; }

    /// <summary>Threads one workflow execution across ASP.NET, Redis, the Python
    /// Runtime, SignalR, and any future worker (Phase 1.5 §2 Correlation IDs).</summary>
    public Guid? CorrelationId { get; init; }

    public required string ProducedBy { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required string PayloadJson { get; init; }
    public double? Confidence { get; init; }
    public string? RiskLevel { get; init; }
}
