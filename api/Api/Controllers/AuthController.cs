using System.Security.Claims;
using AiAgentsTeam.Application.Users.Commands;
using AiAgentsTeam.Application.Users.Queries;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Registration/login (Phase 2, "AI Enterprise OS") — the only controller in
/// the solution that requires no prior auth to call, by definition.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string Name, CompanyType CompanyType);
    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new RegisterUserCommand(request.Email, request.Password, request.Name, request.CompanyType), ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new LoginUserCommand(request.Email, request.Password), ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await sender.Send(new GetCurrentUserQuery(userId), ct));
    }
}
