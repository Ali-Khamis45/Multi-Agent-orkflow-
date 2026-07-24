using FluentValidation;

namespace AiAgentsTeam.Application.Memory.Commands;

public sealed class WriteMemoryItemCommandValidator : AbstractValidator<WriteMemoryItemCommand>
{
    public WriteMemoryItemCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ScopeRef).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Layer).IsInEnum();
        RuleFor(x => x.Kind).IsInEnum();
    }
}
