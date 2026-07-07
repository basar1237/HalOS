using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.ValueObjects;

namespace HalOS.Sales.Application.Abstractions;

/// <summary>
/// Satış anındaki kesinti oran kümesini (<see cref="RateSet"/>) çözen port (docs/02 §4:
/// "oranlar tenant + tarih + taraf bazında yapılandırılır"). Oranlar tenant config'ten ve
/// müstahsilin stopaj profilinden (Party WithholdingProfile) türetilir; böylece satış kendi
/// anındaki oranlarla dondurulur.
///
/// Varsayılan uygulama <c>DefaultRateProvider</c>, müstahsile-özel oran okuma modelini
/// (<see cref="IProducerRateProfileReader"/>) tenant config'e TERCİH eder: okuma modeli Party
/// servisinden gelen <c>ProducerWithholdingProfileChanged</c> event'iyle senkronlanır
/// (docs/02 §6, hakediş doğruluğu). Profil yoksa/eksikse tenant config varsayılanına düşülür;
/// komisyon Party'de tutulmadığından her zaman config'ten gelir.
/// </summary>
public interface IRateProvider
{
    /// <summary>
    /// Verilen satış bağlamı için oran kümesini çözer. <paramref name="isWithinMarket"/> rüsum
    /// oranını (%1/%2) belirler (BK-5). Komisyon %8'i aşarsa veya oran negatifse başarısız döner
    /// (RateSet değişmezi, BK-1).
    /// </summary>
    Task<Result<RateSet>> ResolveAsync(
        Guid tenantId,
        Guid producerPartyId,
        DateTime soldAt,
        bool isWithinMarket,
        CancellationToken cancellationToken = default);
}
