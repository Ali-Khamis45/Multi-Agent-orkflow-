using AiAgentsTeam.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class WorkflowRunConfiguration : IEntityTypeConfiguration<WorkflowRun>
{
    public void Configure(EntityTypeBuilder<WorkflowRun> builder)
    {
        builder.ToTable("workflow_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Goal).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.WorkspaceId);

        // WorkflowRun is the aggregate root for TaskNode/TaskEdge (ARCHITECTURE.md §5.1);
        // Nodes/Edges are exposed as IReadOnlyCollection with no public setter, so EF Core
        // materializes directly into the private `_nodes`/`_edges` backing fields.
        builder.HasMany(x => x.Nodes)
            .WithOne()
            .HasForeignKey(n => n.WorkflowRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(WorkflowRun.Nodes))!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Edges)
            .WithOne()
            .HasForeignKey(e => e.WorkflowRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(WorkflowRun.Edges))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
