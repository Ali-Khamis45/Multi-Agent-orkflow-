using AiAgentsTeam.Domain.Reasoning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ReasoningTraceConfiguration : IEntityTypeConfiguration<ReasoningTrace>
{
    public void Configure(EntityTypeBuilder<ReasoningTrace> builder)
    {
        builder.ToTable("reasoning_traces");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Agent).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ModelUsed).HasMaxLength(100);
        builder.HasIndex(x => new { x.TaskNodeId, x.Stage });
        builder.HasIndex(x => x.WorkflowRunId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.Agent);
    }
}
