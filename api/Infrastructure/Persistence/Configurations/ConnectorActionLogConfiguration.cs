using AiAgentsTeam.Domain.Connectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ConnectorActionLogConfiguration : IEntityTypeConfiguration<ConnectorActionLog>
{
    public void Configure(EntityTypeBuilder<ConnectorActionLog> builder)
    {
        builder.ToTable("connector_action_logs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WorkspaceId, x.ConnectorKey, x.CreatedAt });
        builder.Property(x => x.ConnectorKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ActionKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.InputJson).IsRequired();
    }
}
