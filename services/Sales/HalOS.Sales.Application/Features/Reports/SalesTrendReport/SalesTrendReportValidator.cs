using FluentValidation;

namespace HalOS.Sales.Application.Features.Reports.SalesTrendReport;

/// <summary>Satış trend raporu tarih aralığı doğrulaması: From &lt;= To (docs/07 §5).</summary>
public sealed class SalesTrendReportValidator : AbstractValidator<SalesTrendReportQuery>
{
    public SalesTrendReportValidator()
    {
        RuleFor(x => x.FromUtc)
            .LessThanOrEqualTo(x => x.ToUtc)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}
