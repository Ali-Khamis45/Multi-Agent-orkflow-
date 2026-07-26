using AiAgentsTeam.Domain.Founders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAgentsTeam.Infrastructure.Persistence.Configurations;

public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ToTable("company_profiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.WorkspaceId).IsUnique();
        builder.Property(x => x.ProfileJson).HasColumnType("jsonb").IsRequired();

        // Postgres system column, used as the optimistic-concurrency token for
        // PatchCompanyProfileSectionCommand's read-modify-write retry loop — parallel
        // DAG branches (e.g. Market + Customer Research) can genuinely race to patch
        // different sections of the same row at the same time.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion().ValueGeneratedOnAddOrUpdate();
    }
}
