using FluentValidation;

namespace AiAgentsTeam.Application.Founders.Commands;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ProfileJson).NotEmpty();
    }
}
