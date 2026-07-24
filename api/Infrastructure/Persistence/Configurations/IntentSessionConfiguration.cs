using AiAgentsTeam.Domain.Intent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class IntentSessionConfiguration : IEntityTypeConfiguration<IntentSession>
{
    public void Configure(EntityTypeBuilder<IntentSession> builder)
    {
        builder.ToTable("intent_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RawInput).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => x.WorkspaceId);

        builder.HasMany(x => x.Answers)
            .WithOne()
            .HasForeignKey(a => a.IntentSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(IntentSession.Answers))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
