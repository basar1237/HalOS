using FluentValidation;

namespace HalOS.Sales.Application.Features.Reports.SalesSummaryReport;

/// <summary>Satış özet raporu tarih aralığı doğrulaması: From &lt;= To (docs/07 §5).</summary>
public sealed class SalesSummaryReportValidator : AbstractValidator<SalesSummaryReportQuery>
{
    public SalesSummaryReportValidator()
    {
        RuleFor(x => x.FromUtc)
            .LessThanOrEqualTo(x => x.ToUtc)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}
