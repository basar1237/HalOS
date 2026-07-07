using FluentAssertions;
using HalOS.Sales.Domain.ValueObjects;
using Xunit;

namespace HalOS.Sales.Tests.Domain;

/// <summary>
/// RateSet value object doğrulaması (docs/03 §4 BK-1). Komisyon %8'i aşamaz; oranlar negatif
/// olamaz; rüsum oranı hal içi %1 / hal dışı %2 (BK-5). Oranlar decimal (asla float — BK-2).
/// </summary>
public sealed class RateSetTests
{
    [Fact]
    public void Create_ValidRates_WithinMarket_MarketFeeIsOnePercent()
    {
        // Hal İÇİ satış → rüsum %1 (BK-5).
        var result = RateSet.Create(0.08m, 0.02m, 0.01m, isWithinMarket: true, vatRate: 0.20m);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionRate.Should().Be(0.08m);
        result.Value.MarketFeeRate.Should().Be(0.01m);
    }

    [Fact]
    public void Create_OutsideMarket_MarketFeeIsTwoPercent()
    {
        // Hal DIŞI satış → rüsum %2 (BK-5).
        var result = RateSet.Create(0.08m, 0.02m, 0.01m, isWithinMarket: false, vatRate: 0.20m);

        result.IsSuccess.Should().BeTrue();
        result.Value.MarketFeeRate.Should().Be(0.02m);
    }

    [Fact]
    public void Create_CommissionAboveEightPercent_Fails()
    {
        // Komisyon %8'i AŞAMAZ (BK-1) — değişmez.
        var result = RateSet.Create(0.0801m, 0.02m, 0.01m, isWithinMarket: true, vatRate: 0.20m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RateSetErrors.CommissionRateTooHigh);
    }

    [Fact]
    public void Create_CommissionExactlyEightPercent_Succeeds()
    {
        // Sınır dahil: %8 tam olarak geçerli.
        var result = RateSet.Create(0.08m, 0.02m, 0.01m, isWithinMarket: true, vatRate: 0.20m);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.01, 0.02, 0.01, 0.20)]
    [InlineData(0.08, -0.02, 0.01, 0.20)]
    [InlineData(0.08, 0.02, -0.01, 0.20)]
    [InlineData(0.08, 0.02, 0.01, -0.20)]
    public void Create_NegativeRate_Fails(decimal commission, decimal agri, decimal ssk, decimal vat)
    {
        var result = RateSet.Create(commission, agri, ssk, isWithinMarket: true, vatRate: vat);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RateSetErrors.NegativeRate);
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value;
        var b = RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value;

        a.Should().Be(b);
    }
}
