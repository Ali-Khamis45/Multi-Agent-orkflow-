using FluentValidation;

namespace AiAgentsTeam.Application.Workflows.Commands;

public sealed class CreateWorkflowRunCommandValidator : AbstractValidator<CreateWorkflowRunCommand>
{
    public CreateWorkflowRunCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Goal).NotEmpty().MaximumLength(5000);
    }
}
