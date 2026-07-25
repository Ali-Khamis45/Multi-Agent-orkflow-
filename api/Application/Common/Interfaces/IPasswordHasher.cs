namespace AiAgentsTeam.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure via ASP.NET Core Identity's PasswordHasher&lt;T&gt;
/// (PBKDF2) — Application never touches a hashing algorithm directly.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
