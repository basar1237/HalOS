using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace HalOS.Sales.Application.Rates;

/// <summary>
/// <see cref="IRateProvider"/>'ın varsayılan uygulaması (docs/02 §4). Oranları önce müstahsile-özel
/// okuma modelinden (<see cref="IProducerRateProfileReader"/>) çözer; okuma modeli, Party servisinden
/// gelen <c>ProducerWithholdingProfileChanged</c> event'iyle senkronlanır (docs/02 §6, hakediş
/// doğruluğu). Bulunan alanlar (zirai stopaj + çiftçi Bağ-Kur) kullanılır; müstahsil profili YOKSA
/// veya bir alan yoksa <see cref="RateOptions"/> tenant config varsayılanına düşülür. Komisyon
/// Party'de tutulmadığından her zaman config'ten gelir. Rüsum oranını satışın hal içi/dışı
/// durumuna göre RateSet belirler (%1/%2, BK-5). Komisyon %8 tavanı ve negatif oran kontrolü
/// <see cref="RateSet.Create"/> içinde korunur (BK-1).
/// </summary>
public sealed class DefaultRateProvider : IRateProvider
{
    private readonly RateOptions _options;
    private readonly IProducerRateProfileReader _profiles;

    public DefaultRateProvider(IOptions<RateOptions> options, IProducerRateProfileReader profiles)
    {
        _options = options.Value;
        _profiles = profiles;
    }

    public async Task<Result<RateSet>> ResolveAsync(
        Guid tenantId,
        Guid producerPartyId,
        DateTime soldAt,
        bool isWithinMarket,
        CancellationToken cancellationToken = default)
    {
        // Müstahsile-özel oran profili (Party senkronu) varsa onu tercih et; yoksa/eksikse tenant
        // config varsayılanına düş (docs/02 §4/§6). Profil sorgusu tenant filtreli (BK-8).
        var profile = await _profiles.FindAsync(producerPartyId, cancellationToken);

        var agriWithholdingRate = profile?.AgriWithholdingRate ?? _options.AgriWithholdingRate;
        var farmerSskRate = profile?.FarmerSskRate ?? _options.FarmerSskRate;

        return RateSet.Create(
            _options.DefaultCommissionRate,
            agriWithholdingRate,
            farmerSskRate,
            isWithinMarket,
            _options.CommissionVatRate);
    }
}
