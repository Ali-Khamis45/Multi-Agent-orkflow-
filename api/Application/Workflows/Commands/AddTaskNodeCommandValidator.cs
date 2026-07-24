using FluentValidation;

namespace AiAgentsTeam.Application.Workflows.Commands;

public sealed class AddTaskNodeCommandValidator : AbstractValidator<AddTaskNodeCommand>
{
    public AddTaskNodeCommandValidator()
    {
        RuleFor(x => x.WorkflowRunId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaskType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.InputsJson).NotEmpty().Must(BeValidJson).WithMessage("InputsJson must be valid JSON.");
    }

    private static bool BeValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
