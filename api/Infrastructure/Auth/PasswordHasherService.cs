using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace AiAgentsTeam.Infrastructure.Auth;

/// <summary>PBKDF2 via ASP.NET Core Identity's PasswordHasher&lt;T&gt; — battle-tested,
/// no custom crypto. The generic type argument only needs to be *a* reference type;
/// it's never persisted or compared, so reusing the domain User is just convenient,
/// not a real dependency on User's shape.</summary>
public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(default!, password);

    public bool Verify(string passwordHash, string providedPassword) =>
        _inner.VerifyHashedPassword(default!, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
