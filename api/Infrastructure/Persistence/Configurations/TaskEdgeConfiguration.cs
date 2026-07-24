using AiAgentsTeam.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class TaskEdgeConfiguration : IEntityTypeConfiguration<TaskEdge>
{
    public void Configure(EntityTypeBuilder<TaskEdge> builder)
    {
        builder.ToTable("task_edges");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PredecessorNodeId);
        builder.HasIndex(x => x.SuccessorNodeId);
    }
}
