using AiAgentsTeam.Domain.Founders;
using FluentValidation;

namespace AiAgentsTeam.Application.Founders.Commands;

public sealed class PatchCompanyProfileSectionCommandValidator : AbstractValidator<PatchCompanyProfileSectionCommand>
{
    public PatchCompanyProfileSectionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Section).Must(CompanyProfileJson.Sections.Contains)
            .WithMessage($"Section must be one of: {string.Join(", ", CompanyProfileJson.Sections)}.");
        RuleFor(x => x.PatchJson).NotEmpty();
    }
}
