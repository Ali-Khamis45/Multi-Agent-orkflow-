using AiAgentsTeam.Application.Connectors.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

public sealed class CredentialProtector : ICredentialProtector
{
    private readonly IDataProtector _protector;

    public CredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("AiAgentsTeam.ConnectorCredentials.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
