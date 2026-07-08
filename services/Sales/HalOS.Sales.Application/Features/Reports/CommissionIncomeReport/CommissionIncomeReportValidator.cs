using FluentValidation;

namespace HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;

/// <summary>Komisyon geliri raporu tarih aralığı doğrulaması: From &lt;= To (docs/07 §5).</summary>
public sealed class CommissionIncomeReportValidator : AbstractValidator<CommissionIncomeReportQuery>
{
    public CommissionIncomeReportValidator()
    {
        RuleFor(x => x.FromUtc)
            .LessThanOrEqualTo(x => x.ToUtc)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}
