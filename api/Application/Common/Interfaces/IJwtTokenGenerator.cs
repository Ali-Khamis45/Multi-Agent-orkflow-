using AiAgentsTeam.Domain.Users;

namespace AiAgentsTeam.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure. Issues the bearer token the frontend
/// attaches to every authenticated request; claims carry UserId and CompanyType so
/// [Authorize] policies can route/restrict without a database round-trip.</summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
