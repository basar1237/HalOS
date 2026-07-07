namespace HalOS.Sales.Application.Abstractions;

/// <summary>
/// Müstahsile-özel oran okuma modelini (ProducerRateProfile) çözen port (docs/02 §6). Read-model,
/// Party servisinden gelen <c>ProducerWithholdingProfileChanged</c> event'iyle senkronlanır ve
/// <see cref="IRateProvider"/> tarafından satış anında okunur. Sorgu tenant global query filter'a
/// tabidir (BK-8); yalnızca geçerli tenant'ın müstahsili döner.
/// </summary>
public interface IProducerRateProfileReader
{
    /// <summary>
    /// Verilen müstahsil için senkronlanmış oran profilini getirir; yoksa <c>null</c> döner
    /// (bu durumda çağıran tenant config varsayılanına düşer).
    /// </summary>
    Task<ProducerRateSnapshot?> FindAsync(
        Guid producerPartyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="IProducerRateProfileReader"/>'ın döndürdüğü salt-okunur oran anlık görüntüsü.
/// Yalnızca Party'de GERÇEKTEN var olan oranları taşır (zirai stopaj + çiftçi Bağ-Kur); komisyon
/// Party'de tutulmadığından burada yer almaz (tenant config'ten çözülür — docs/02 §4).
/// </summary>
public sealed record ProducerRateSnapshot(
    decimal AgriWithholdingRate,
    decimal FarmerSskRate);
