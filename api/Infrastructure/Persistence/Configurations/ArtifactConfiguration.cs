using AiAgentsTeam.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OwnerAgent).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => new { x.WorkspaceId, x.Name });
        builder.HasIndex(x => x.PreviousVersionId);
        builder.HasIndex(x => x.CorrelationId);

        // Idempotency (Phase 1.5 §3): a retried produce_artifact call with the same
        // key must resolve to the same row, not a spurious extra version. Partial
        // index — most artifacts (manual/ad hoc creation) have no IdempotencyKey.
        builder.HasIndex(x => new { x.WorkflowRunId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}
