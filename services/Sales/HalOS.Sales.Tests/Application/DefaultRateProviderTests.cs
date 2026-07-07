using FluentAssertions;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Rates;
using Microsoft.Extensions.Options;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// DefaultRateProvider testleri (docs/02 §4/§6). Oranları önce müstahsile-özel okuma modelinden
/// (Party senkronu) çözer, eksikte tenant config'e düşer; rüsum oranını hal içi/dışı belirler
/// (BK-5); komisyon %8 sınırını RateSet üzerinden korur (BK-1).
/// </summary>
public sealed class DefaultRateProviderTests
{
    /// <summary>Testler için ayarlanabilir okuma modeli stub'ı (Party senkronunu taklit eder).</summary>
    private sealed class StubProducerRateProfileReader : IProducerRateProfileReader
    {
        private readonly ProducerRateSnapshot? _snapshot;

        public StubProducerRateProfileReader(ProducerRateSnapshot? snapshot) => _snapshot = snapshot;

        public Task<ProducerRateSnapshot?> FindAsync(
            Guid producerPartyId,
            CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
    }

    private static DefaultRateProvider Create(RateOptions options, ProducerRateSnapshot? profile = null) =>
        new(Options.Create(options), new StubProducerRateProfileReader(profile));

    [Fact]
    public async Task Resolve_WithinMarket_UsesConfigRatesAndOnePercentFee()
    {
        // Müstahsil profili YOK → config varsayılanları kullanılır.
        var provider = Create(new RateOptions
        {
            DefaultCommissionRate = 0.08m,
            AgriWithholdingRate = 0.02m,
            FarmerSskRate = 0.01m,
            CommissionVatRate = 0.20m
        });

        var result = await provider.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, isWithinMarket: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommissionRate.Should().Be(0.08m);
        result.Value.AgriWithholdingRate.Should().Be(0.02m);
        result.Value.FarmerSskRate.Should().Be(0.01m);
        result.Value.MarketFeeRate.Should().Be(0.01m);
    }

    [Fact]
    public async Task Resolve_OutsideMarket_UsesTwoPercentFee()
    {
        var provider = Create(new RateOptions());

        var result = await provider.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, isWithinMarket: false);

        result.Value.MarketFeeRate.Should().Be(0.02m);
    }

    [Fact]
    public async Task Resolve_CommissionAboveEightPercent_Fails()
    {
        // Config komisyonu %8'i aşarsa RateSet reddeder (BK-1) → provider hata döner.
        var provider = Create(new RateOptions { DefaultCommissionRate = 0.09m });

        var result = await provider.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, isWithinMarket: true);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Resolve_ProducerProfileExists_PrefersProfileOverConfig()
    {
        // Müstahsil profili config'ten FARKLI oranlar taşıyorsa profil tercih edilir (docs/02 §6).
        var provider = Create(
            new RateOptions
            {
                DefaultCommissionRate = 0.08m,
                AgriWithholdingRate = 0.02m,
                FarmerSskRate = 0.01m,
                CommissionVatRate = 0.20m
            },
            profile: new ProducerRateSnapshot(AgriWithholdingRate: 0.04m, FarmerSskRate: 0.03m));

        var result = await provider.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, isWithinMarket: true);

        result.IsSuccess.Should().BeTrue();
        // Stopaj/Bağ-Kur profilden gelir; komisyon Party'de tutulmadığından config'ten gelir.
        result.Value.AgriWithholdingRate.Should().Be(0.04m);
        result.Value.FarmerSskRate.Should().Be(0.03m);
        result.Value.CommissionRate.Should().Be(0.08m);
    }

    [Fact]
    public async Task Resolve_NoProducerProfile_FallsBackToConfig()
    {
        // Profil yoksa (null) tüm oran alanları config varsayılanına düşer (docs/02 §6 fallback).
        var provider = Create(
            new RateOptions
            {
                DefaultCommissionRate = 0.08m,
                AgriWithholdingRate = 0.02m,
                FarmerSskRate = 0.01m,
                CommissionVatRate = 0.20m
            },
            profile: null);

        var result = await provider.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, isWithinMarket: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgriWithholdingRate.Should().Be(0.02m);
        result.Value.FarmerSskRate.Should().Be(0.01m);
    }
}
