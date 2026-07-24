using AiAgentsTeam.Domain.Intent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class ClarificationAnswerConfiguration : IEntityTypeConfiguration<ClarificationAnswer>
{
    public void Configure(EntityTypeBuilder<ClarificationAnswer> builder)
    {
        builder.ToTable("clarification_answers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).IsRequired();
        builder.Property(x => x.Answer).IsRequired();
    }
}
