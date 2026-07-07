using FluentAssertions;
using HalOS.Finance.Application.Features.RecordAdvance;
using HalOS.Finance.Application.Features.RecordCollection;
using HalOS.Finance.Application.Features.RecordPayment;
using HalOS.Finance.Domain.Enums;
using Xunit;

namespace HalOS.Finance.Tests.Application;

/// <summary>
/// BK-6 nakit eşiği (7.000 TL) validator seviyesinde erken/net uyarı verir (docs/07 §5 validasyon
/// pipeline'ı). Nakit &gt; 7.000 reddedilir; banka her tutarda geçer; nakit ≤ 7.000 geçer.
/// </summary>
public sealed class CashLimitValidatorTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private readonly Guid _partyId = Guid.NewGuid();

    [Fact]
    public void Payment_CashAboveLimit_IsInvalid()
    {
        var validator = new RecordPaymentValidator();
        var result = validator.Validate(
            new RecordPaymentCommand(_partyId, 7000.01m, PaymentChannel.Cash, null, Now));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("7.000 TL"));
    }

    [Fact]
    public void Payment_CashAtLimit_IsValid()
    {
        var validator = new RecordPaymentValidator();
        validator.Validate(new RecordPaymentCommand(_partyId, 7000.00m, PaymentChannel.Cash, null, Now))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Payment_BankAboveLimit_IsValid()
    {
        var validator = new RecordPaymentValidator();
        validator.Validate(new RecordPaymentCommand(_partyId, 50000.00m, PaymentChannel.Bank, "REF", Now))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Collection_CashAboveLimit_IsInvalid()
    {
        new RecordCollectionValidator()
            .Validate(new RecordCollectionCommand(_partyId, 9000m, PaymentChannel.Cash, null, Now))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Advance_CashAboveLimit_IsInvalid()
    {
        new RecordAdvanceValidator()
            .Validate(new RecordAdvanceCommand(_partyId, 9000m, PaymentChannel.Cash, null, Now))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Payment_ZeroAmount_IsInvalid()
    {
        new RecordPaymentValidator()
            .Validate(new RecordPaymentCommand(_partyId, 0m, PaymentChannel.Bank, null, Now))
            .IsValid.Should().BeFalse();
    }
}
