using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Users.Queries;

public sealed record CurrentUserDto(Guid UserId, string Email, string Name, string CompanyType);

/// <summary>Backs GET /api/auth/me — how the frontend re-establishes session state
/// (name, company type) from a stored token on page load/refresh.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        return new CurrentUserDto(user.Id, user.Email, user.Name, user.CompanyType.ToString());
    }
}
