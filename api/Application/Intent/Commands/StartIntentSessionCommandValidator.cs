using FluentValidation;

namespace AiAgentsTeam.Application.Intent.Commands;

public sealed class StartIntentSessionCommandValidator : AbstractValidator<StartIntentSessionCommand>
{
    public StartIntentSessionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.RawInput).NotEmpty().MaximumLength(20_000);
    }
}
