using AiAgentsTeam.Domain.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class AgentRegistrationConfiguration : IEntityTypeConfiguration<AgentRegistration>
{
    public void Configure(EntityTypeBuilder<AgentRegistration> builder)
    {
        builder.ToTable("agent_registrations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Version).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Endpoint).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        // Npgsql maps List<string> to a native Postgres text[] column.
        builder.Property(x => x.Skills).IsRequired();
        builder.Property(x => x.SupportedTasks).IsRequired();
        builder.Property(x => x.RequiredContext).IsRequired();
        builder.Property(x => x.ProducedArtifacts).IsRequired();
        builder.Property(x => x.Dependencies).IsRequired();
        builder.Property(x => x.Tools).IsRequired();
        builder.Property(x => x.Permissions).IsRequired();
    }
}
