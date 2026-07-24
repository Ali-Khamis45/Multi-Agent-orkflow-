using AiAgentsTeam.Domain.Failures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ExecutionFailureConfiguration : IEntityTypeConfiguration<ExecutionFailure>
{
    public void Configure(EntityTypeBuilder<ExecutionFailure> builder)
    {
        builder.ToTable("execution_failures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Agent).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Message).IsRequired();
        builder.HasIndex(x => x.TaskNodeId);
        builder.HasIndex(x => x.WorkflowRunId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => new { x.Agent, x.Category });
    }
}
