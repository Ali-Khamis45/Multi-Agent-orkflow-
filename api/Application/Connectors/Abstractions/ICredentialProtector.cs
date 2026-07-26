namespace AiAgentsTeam.Application.Connectors.Abstractions;

/// <summary>Encrypts connector credentials (OAuth tokens, API keys) before they reach
/// persistence, and decrypts them right before a connector call needs them — the only
/// place plaintext secrets exist is in memory, for the duration of one request.
/// Implemented in Infrastructure via ASP.NET Core Data Protection.</summary>
public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}
