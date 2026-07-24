using FluentValidation;
using MediatR;

namespace AiAgentsTeam.Application.Common.Behaviours;

/// <summary>
/// API Contract Validation (Phase 1.5 §10): every command runs through its
/// registered FluentValidation validators (if any) before its handler executes —
/// invalid payloads are rejected here, uniformly, rather than each handler
/// re-implementing its own guard clauses. A command with no registered validator
/// passes through unchanged (most queries; simple commands with no invariants
/// beyond what the domain constructor already enforces).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
