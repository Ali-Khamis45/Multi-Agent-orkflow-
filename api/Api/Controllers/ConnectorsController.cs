using System.Security.Claims;
using AiAgentsTeam.Application.Connectors.Commands;
using AiAgentsTeam.Application.Connectors.Queries;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>
/// Phase 4 "Connector Framework" — browse/install/disconnect/health/sync/execute for
/// every registered connector. Dual-auth on the calls a Founder/Software agent also
/// needs to make with no user session (health/sync/execute-action), same pattern as
/// CompanyProfileController; catalog/install/disconnect/authorize-url are user-driven
/// only. The OAuth callback is unauthenticated by necessity (it's a browser redirect
/// from the third-party provider) — its security is the signed `state` param, not a
/// bearer token.
/// </summary>
[ApiController]
[Route("api/connectors")]
public sealed class ConnectorsController(ISender sender, IOptions<InternalServiceOptions> internalOptions, IConfiguration configuration) : ControllerBase
{
    public sealed record InstallRequest(Guid WorkspaceId, Dictionary<string, string> Credentials);
    public sealed record WorkspaceRequest(Guid WorkspaceId);
    public sealed record ExecuteActionRequest(Guid WorkspaceId, string InputJson);

    private const string ServiceKeyHeader = "X-Internal-Service-Key";

    private CompanyType CurrentCompanyType => Enum.Parse<CompanyType>(User.FindFirstValue("company_type")!);

    private bool IsServiceOrAuthenticated =>
        (Request.Headers.TryGetValue(ServiceKeyHeader, out var key) && key == internalOptions.Value.ServiceKey)
        || User.Identity?.IsAuthenticated == true;

    [Authorize]
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<ConnectorCatalogEntryDto>>> GetCatalog(CancellationToken ct) =>
        Ok(await sender.Send(new GetConnectorCatalogQuery(CurrentCompanyType), ct));

    [Authorize]
    [HttpGet("installed")]
    public async Task<ActionResult<IReadOnlyList<InstalledConnectorDto>>> GetInstalled([FromQuery] Guid workspaceId, CancellationToken ct) =>
        Ok(await sender.Send(new GetInstalledConnectorsQuery(workspaceId), ct));

    [Authorize]
    [HttpPost("{key}/install")]
    public async Task<IActionResult> Install(string key, InstallRequest request, CancellationToken ct)
    {
        await sender.Send(new InstallConnectorCommand(request.WorkspaceId, key, request.Credentials), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{key}/disconnect")]
    public async Task<IActionResult> Disconnect(string key, WorkspaceRequest request, CancellationToken ct)
    {
        await sender.Send(new DisconnectConnectorCommand(request.WorkspaceId, key), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("{key}/oauth/authorize-url")]
    public async Task<ActionResult<string>> GetAuthorizeUrl(string key, [FromQuery] Guid workspaceId, CancellationToken ct)
    {
        var url = await sender.Send(new GetConnectorAuthorizeUrlQuery(workspaceId, key), ct);
        return Ok(new { url });
    }

    /// <summary>No [Authorize]: this is a browser redirect landing straight from the
    /// third-party provider, so there's no bearer token — the signed `state` param
    /// (see CompleteConnectorOAuthCommand) is what proves which workspace this belongs to.</summary>
    [HttpGet("{key}/oauth/callback")]
    public async Task<IActionResult> OAuthCallback(string key, [FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        var frontendBaseUrl = configuration["Connectors:FrontendBaseUrl"] ?? "http://localhost:3000";
        try
        {
            await sender.Send(new CompleteConnectorOAuthCommand(key, code, state), ct);
            return Redirect($"{frontendBaseUrl}/founder/connectors?connected={Uri.EscapeDataString(key)}");
        }
        catch (Exception)
        {
            return Redirect($"{frontendBaseUrl}/founder/connectors?error={Uri.EscapeDataString(key)}");
        }
    }

    [HttpPost("{key}/health")]
    public async Task<ActionResult<ConnectorHealthDto>> CheckHealth(string key, WorkspaceRequest request, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new CheckConnectorHealthCommand(request.WorkspaceId, key), ct));
    }

    [HttpPost("{key}/sync")]
    public async Task<ActionResult<ConnectorSyncDto>> Sync(string key, WorkspaceRequest request, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new SyncConnectorCommand(request.WorkspaceId, key), ct));
    }

    [HttpPost("{key}/actions/{actionKey}")]
    public async Task<ActionResult<ConnectorActionDto>> ExecuteAction(string key, string actionKey, ExecuteActionRequest request, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new ExecuteConnectorActionCommand(request.WorkspaceId, key, actionKey, request.InputJson), ct));
    }
}
