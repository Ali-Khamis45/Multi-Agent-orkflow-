using AiAgentsTeam.Domain.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class MemoryItemConfiguration : IEntityTypeConfiguration<MemoryItem>
{
    public void Configure(EntityTypeBuilder<MemoryItem> builder)
    {
        builder.ToTable("memory_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Layer).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.Layer, x.ScopeRef });
    }
}
