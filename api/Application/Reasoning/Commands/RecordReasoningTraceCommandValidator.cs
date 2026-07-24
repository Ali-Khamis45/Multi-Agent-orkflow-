using FluentValidation;

namespace AiAgentsTeam.Application.Reasoning.Commands;

public sealed class RecordReasoningTraceCommandValidator : AbstractValidator<RecordReasoningTraceCommand>
{
    public RecordReasoningTraceCommandValidator()
    {
        RuleFor(x => x.TaskNodeId).NotEmpty();
        RuleFor(x => x.Agent).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Stage).IsInEnum();
        RuleFor(x => x.DurationMs).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RetryCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MemoryReads).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MemoryWrites).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ToolCalls).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Confidence).InclusiveBetween(0.0, 1.0).When(x => x.Confidence.HasValue);
    }
}
