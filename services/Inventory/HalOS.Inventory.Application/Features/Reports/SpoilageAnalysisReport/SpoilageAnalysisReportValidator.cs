using FluentValidation;

namespace HalOS.Inventory.Application.Features.Reports.SpoilageAnalysisReport;

/// <summary>
/// Fire analizi raporu doğrulaması (docs/07 §5). Başlangıç ve bitiş tarihleri zorunlu (default
/// DateTime olamaz) ve başlangıç bitişten sonra olamaz. Finance rapor validator deseniyle birebir.
/// </summary>
public sealed class SpoilageAnalysisReportValidator : AbstractValidator<SpoilageAnalysisReportQuery>
{
    public SpoilageAnalysisReportValidator()
    {
        RuleFor(x => x.FromUtc).NotEmpty().WithMessage("Başlangıç tarihi (from) zorunludur.");
        RuleFor(x => x.ToUtc).NotEmpty().WithMessage("Bitiş tarihi (to) zorunludur.");
        RuleFor(x => x.FromUtc)
            .LessThanOrEqualTo(x => x.ToUtc)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}
