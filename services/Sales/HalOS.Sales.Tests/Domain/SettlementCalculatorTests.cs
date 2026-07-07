using FluentAssertions;
using HalOS.Sales.Domain.Services;
using HalOS.Sales.Domain.ValueObjects;
using Xunit;

namespace HalOS.Sales.Tests.Domain;

/// <summary>
/// Kesinti/hakediş motorunun (SistemİN KALBİ) saf birim testleri (docs/02 §4, docs/03 §4
/// BK-1/BK-2). In-memory, dış altyapısız (docs/07 §7). BK-1 88 TL senaryosu ve BK-2 yuvarlama
/// örneği MUTLAKA burada.
/// </summary>
public sealed class SettlementCalculatorTests
{
    private static RateSet WithinMarketRates(
        decimal commission = 0.08m,
        decimal agri = 0.02m,
        decimal ssk = 0.01m,
        decimal vat = 0.20m) =>
        RateSet.Create(commission, agri, ssk, isWithinMarket: true, vatRate: vat).Value;

    [Fact]
    public void Calculate_BK1_HundredGrossWithinMarketEightPercent_NetIs88()
    {
        // docs/03 §4 BK-1 birebir örnek: gross=100, hal içi, komisyon %8.
        // 100 − 8 − 2 − 1 − 1 = 88,00 TL müstahsile.
        var calc = SettlementCalculator.Calculate(100m, WithinMarketRates());

        calc.Gross.Should().Be(100m);
        calc.Commission.Should().Be(8.00m);
        calc.AgriWithholding.Should().Be(2.00m);
        calc.FarmerSsk.Should().Be(1.00m);
        calc.MarketFee.Should().Be(1.00m); // hal içi %1 (BK-5).
        calc.Net.Should().Be(88.00m);
    }

    [Fact]
    public void Calculate_BK1_VatOnCommission_IsNotDeductedFromNet()
    {
        // Komisyon KDV'si (komisyon 8 × %20 = 1,60) HESAPLANIR ama hakedişten DÜŞÜLMEZ (BK-1).
        var calc = SettlementCalculator.Calculate(100m, WithinMarketRates(vat: 0.20m));

        calc.VatOnCommission.Should().Be(1.60m);
        // Net hâlâ 88; KDV net'i etkilemez.
        calc.Net.Should().Be(88.00m);
        // Düşülen kesintiler toplamı KDV hariç: 8 + 2 + 1 + 1 = 12.
        calc.TotalDeductions.Should().Be(12.00m);
    }

    [Fact]
    public void Calculate_OutsideMarket_MarketFeeIsTwoPercent()
    {
        // Hal DIŞI satış → rüsum %2 (BK-5). gross=100 → rüsum 2 → net 100−8−2−1−2 = 87.
        var rates = RateSet.Create(0.08m, 0.02m, 0.01m, isWithinMarket: false, vatRate: 0.20m).Value;

        var calc = SettlementCalculator.Calculate(100m, rates);

        calc.MarketFee.Should().Be(2.00m);
        calc.Net.Should().Be(87.00m);
    }

    [Fact]
    public void Calculate_BK2_Rounding_UsesBankersRoundingToEven()
    {
        // docs/03 §4 BK-2: kuruş, MidpointRounding.ToEven. gross=33.33.
        // commission = 33.33 × 0.08 = 2.6664 → Round(2 hane, ToEven) = 2.67.
        // agri       = 33.33 × 0.02 = 0.6666 → 0.67.
        // ssk        = 33.33 × 0.01 = 0.3333 → 0.33.
        // marketFee  = 33.33 × 0.01 = 0.3333 → 0.33.
        // net = 33.33 − (2.67 + 0.67 + 0.33 + 0.33) = 33.33 − 4.00 = 29.33.
        var calc = SettlementCalculator.Calculate(33.33m, WithinMarketRates());

        calc.Commission.Should().Be(2.67m);
        calc.AgriWithholding.Should().Be(0.67m);
        calc.FarmerSsk.Should().Be(0.33m);
        calc.MarketFee.Should().Be(0.33m);
        calc.Net.Should().Be(29.33m);
    }

    [Fact]
    public void Calculate_BK2_ToEven_HalfRoundsToNearestEven()
    {
        // Banker's rounding kanıtı: tam yarım (.005) çift komşuya yuvarlanır.
        // gross = 0.625, ssk %1 = 0.00625 → Round(2, ToEven) = 0.01? Hayır: 0.00625 → 0.01
        //   (3. hane 6 > 5). Bunun yerine net midpoint örneği: 2.675 → 2.68 değil 2.68? test.
        // Money.RoundToKurus doğrudan sınanır.
        Money.RoundToKurus(2.665m).Should().Be(2.66m);  // .665 → çift komşu .66
        Money.RoundToKurus(2.675m).Should().Be(2.68m);  // .675 → çift komşu .68
        Money.RoundToKurus(2.685m).Should().Be(2.68m);  // .685 → çift komşu .68
    }

    [Fact]
    public void Calculate_NetIsNeverNegative_ForNormalRates()
    {
        // Normal oranlarla (toplam ≈ %12) net her zaman pozitif.
        var calc = SettlementCalculator.Calculate(1_000_000m, WithinMarketRates());

        calc.Net.Should().BeGreaterThan(0m);
    }
}
