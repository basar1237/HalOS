using FluentValidation;

namespace HalOS.Finance.Application.Features.Reports.CurrentAccountAgingReport;

/// <summary>
/// Cari yaşlandırma raporu doğrulaması (docs/07 §5). Referans tarih (AsOfUtc) zorunlu ve varsayılan
/// (default) DateTime olamaz — geçersiz/eksik tarihte rapor anlamsızdır. Sales rapor validator deseniyle.
/// </summary>
public sealed class CurrentAccountAgingReportValidator : AbstractValidator<CurrentAccountAgingReportQuery>
{
    public CurrentAccountAgingReportValidator()
    {
        RuleFor(x => x.AsOfUtc)
            .NotEmpty()
            .WithMessage("Referans tarihi (asOf) zorunludur.");
    }
}
