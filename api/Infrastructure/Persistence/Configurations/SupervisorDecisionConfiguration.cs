using AiAgentsTeam.Domain.Supervisor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class SupervisorDecisionConfiguration : IEntityTypeConfiguration<SupervisorDecision>
{
    public void Configure(EntityTypeBuilder<SupervisorDecision> builder)
    {
        builder.ToTable("supervisor_decisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DecisionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Rationale).IsRequired();
        builder.Property(x => x.InputSnapshotJson).IsRequired();
        builder.HasIndex(x => x.WorkflowRunId);
        builder.HasIndex(x => x.CorrelationId);
    }
}
