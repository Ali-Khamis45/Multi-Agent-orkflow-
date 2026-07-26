using System.Security.Cryptography;
using System.Text;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

/// <summary>HMAC-signed, self-verifying `state` param — reuses Jwt:Secret rather than
/// adding a second required secret to configuration, since both exist for the same
/// reason (proving a token wasn't tampered with between issuance and use).</summary>
public sealed class ConnectorOAuthStateSigner(IOptions<JwtOptions> jwtOptions) : IConnectorOAuthStateSigner
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public string Sign(Guid workspaceId, string connectorKey)
    {
        var expiresAtUnix = DateTimeOffset.UtcNow.Add(Ttl).ToUnixTimeSeconds();
        var payload = $"{workspaceId}|{connectorKey}|{expiresAtUnix}";
        var signature = Convert.ToHexString(ComputeHmac(payload));
        return Base64Url(payload + "|" + signature);
    }

    public (Guid WorkspaceId, string ConnectorKey)? Verify(string state)
    {
        try
        {
            var decoded = FromBase64Url(state);
            var parts = decoded.Split('|');
            if (parts.Length != 4) return null;

            var (workspaceIdRaw, connectorKey, expiresAtRaw, signature) = (parts[0], parts[1], parts[2], parts[3]);
            var payload = $"{workspaceIdRaw}|{connectorKey}|{expiresAtRaw}";
            var expected = Convert.ToHexString(ComputeHmac(payload));
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(signature), Encoding.UTF8.GetBytes(expected)))
                return null;

            if (!long.TryParse(expiresAtRaw, out var expiresAtUnix) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAtUnix)
                return null;
            if (!Guid.TryParse(workspaceIdRaw, out var workspaceId))
                return null;

            return (workspaceId, connectorKey);
        }
        catch
        {
            return null;
        }
    }

    private byte[] ComputeHmac(string payload) =>
        new HMACSHA256(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret)).ComputeHash(Encoding.UTF8.GetBytes(payload));

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
