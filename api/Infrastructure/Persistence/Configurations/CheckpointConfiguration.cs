using AiAgentsTeam.Domain.Checkpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("checkpoints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SnapshotJson).IsRequired();
        builder.HasIndex(x => x.WorkflowRunId);
        builder.HasIndex(x => x.CorrelationId);
    }
}
