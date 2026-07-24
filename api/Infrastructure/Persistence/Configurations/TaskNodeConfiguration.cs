using AiAgentsTeam.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class TaskNodeConfiguration : IEntityTypeConfiguration<TaskNode>
{
    public void Configure(EntityTypeBuilder<TaskNode> builder)
    {
        builder.ToTable("task_nodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TaskType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.WorkflowRunId, x.Status });

        // Idempotency (Phase 1.5 §3) defense-in-depth: even under a race between
        // two concurrent AddTaskNodeCommand calls that both miss the in-memory
        // existing-name check, the database rejects the second insert outright.
        builder.HasIndex(x => new { x.WorkflowRunId, x.Name }).IsUnique();
        builder.HasIndex(x => x.CorrelationId);
    }
}
