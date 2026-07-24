using FluentValidation;

namespace AiAgentsTeam.Application.Artifacts.Commands;

public sealed class CreateArtifactCommandValidator : AbstractValidator<CreateArtifactCommand>
{
    public CreateArtifactCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OwnerAgent).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
    }
}
