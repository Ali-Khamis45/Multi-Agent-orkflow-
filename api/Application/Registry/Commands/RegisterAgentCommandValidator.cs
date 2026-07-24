using FluentValidation;

namespace AiAgentsTeam.Application.Registry.Commands;

public sealed class RegisterAgentCommandValidator : AbstractValidator<RegisterAgentCommand>
{
    public RegisterAgentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty().Must(BeAValidUri).WithMessage("Endpoint must be a valid absolute URI.");
        RuleFor(x => x.SupportedTasks).NotEmpty().WithMessage("An agent must support at least one task type.");
        RuleFor(x => x.Priority).InclusiveBetween(0, 100);
    }

    private static bool BeAValidUri(string endpoint) => Uri.TryCreate(endpoint, UriKind.Absolute, out _);
}
