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
    }
}
