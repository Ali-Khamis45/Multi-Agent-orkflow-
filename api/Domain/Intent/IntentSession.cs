using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Intent;

/// <summary>
/// The Intent Engine's working state (ARCHITECTURE_EXTENSION.md §E2) — requirement
/// understanding, goal extraction, risk/ambiguity detection, and project
/// classification, run before any WorkflowRun exists.
/// </summary>
public class IntentSession : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string RawInput { get; private set; } = default!;
    public IntentSessionStatus Status { get; private set; } = IntentSessionStatus.Analyzing;

    public string? ExtractedGoalsJson { get; private set; }
    public string? ProjectClassification { get; private set; }
    public double? ComplexityScore { get; private set; }
    public string? RiskFlagsJson { get; private set; }
    public string? AmbiguitiesJson { get; private set; }
    public Guid? StructuredRequirementsArtifactId { get; private set; }

    private readonly List<ClarificationAnswer> _answers = new();
    public IReadOnlyCollection<ClarificationAnswer> Answers => _answers.AsReadOnly();

    private IntentSession() { }

    public IntentSession(Guid workspaceId, string rawInput)
    {
        WorkspaceId = workspaceId;
        RawInput = rawInput;
    }

    public void RecordAnalysis(
        string extractedGoalsJson,
        string projectClassification,
        double complexityScore,
        string riskFlagsJson,
        string ambiguitiesJson,
        bool hasAmbiguity)
    {
        ExtractedGoalsJson = extractedGoalsJson;
        ProjectClassification = projectClassification;
        ComplexityScore = complexityScore;
        RiskFlagsJson = riskFlagsJson;
        AmbiguitiesJson = ambiguitiesJson;
        Status = hasAmbiguity ? IntentSessionStatus.AwaitingClarification : IntentSessionStatus.Structured;
    }

    public ClarificationAnswer AddAnswer(string question, string answer)
    {
        var entry = new ClarificationAnswer(Id, question, answer);
        _answers.Add(entry);
        return entry;
    }

    public void MarkStructured(Guid structuredRequirementsArtifactId)
    {
        StructuredRequirementsArtifactId = structuredRequirementsArtifactId;
        Status = IntentSessionStatus.Structured;
    }
}
