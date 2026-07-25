using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Users;

/// <summary>
/// An account on the platform (Phase 2, "AI Enterprise OS"). Every user picks a
/// <see cref="Users.CompanyType"/> at registration, permanently — it determines which
/// product (Mission Control for SoftwareCompany, the Founder workspace for Founder)
/// they're routed into on every login, and which agents/workflows/artifacts they can
/// see. Email is the natural key; uniqueness is enforced at the database level (see
/// UserConfiguration) since this is the one place in the schema where a duplicate
/// silently corrupts login, not just data integrity.
/// </summary>
public class User : Entity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public CompanyType CompanyType { get; private set; }

    private User() { }

    public User(string email, string passwordHash, string name, CompanyType companyType)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Name = name.Trim();
        CompanyType = companyType;
    }
}
