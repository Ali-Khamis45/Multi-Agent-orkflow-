using AiAgentsTeam.Domain.Connectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ConnectorInstallationConfiguration : IEntityTypeConfiguration<ConnectorInstallation>
{
    public void Configure(EntityTypeBuilder<ConnectorInstallation> builder)
    {
        builder.ToTable("connector_installations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WorkspaceId, x.ConnectorKey }).IsUnique();
        builder.Property(x => x.ConnectorKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}
