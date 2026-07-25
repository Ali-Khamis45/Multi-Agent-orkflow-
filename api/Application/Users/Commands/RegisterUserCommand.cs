using AiAgentsTeam.Application.Common.Exceptions;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Users;
using AiAgentsTeam.Domain.Workspaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Users.Commands;

public sealed record AuthResultDto(Guid UserId, string Email, string Name, string CompanyType, string Token);

public sealed record RegisterUserCommand(string Email, string Password, string Name, CompanyType CompanyType)
    : IRequest<AuthResultDto>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyType).IsInEnum();
    }
}

/// <summary>
/// Registration creates the account and its first Workspace in one step — a brand
/// new user always has somewhere to work immediately, matching Phase 1's original
/// "no manual setup" principle carried into Phase 2's auth flow.
/// </summary>
public sealed class RegisterUserCommandHandler(
    IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
            throw new ConflictException("An account with this email already exists.");

        var passwordHash = hasher.Hash(request.Password);
        var user = new User(normalizedEmail, passwordHash, request.Name, request.CompanyType);
        db.Users.Add(user);
        db.Workspaces.Add(new Workspace("default", user.Id));

        await db.SaveChangesAsync(cancellationToken);

        var token = jwt.GenerateToken(user);
        return new AuthResultDto(user.Id, user.Email, user.Name, user.CompanyType.ToString(), token);
    }
}
