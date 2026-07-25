using AiAgentsTeam.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Users.Commands;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<AuthResultDto>;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserCommandHandler(
    IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    : IRequestHandler<LoginUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Deliberately identical failure for "no such user" and "wrong password" —
        // distinguishing them lets an attacker enumerate registered emails.
        if (user is null || !hasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = jwt.GenerateToken(user);
        return new AuthResultDto(user.Id, user.Email, user.Name, user.CompanyType.ToString(), token);
    }
}
