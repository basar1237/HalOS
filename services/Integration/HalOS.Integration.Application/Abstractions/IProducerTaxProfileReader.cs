using HalOS.Integration.Domain.ReadModels;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// Müstahsil vergi/kayıt profili okuma modeline erişim port'u (docs/02 §6; Party senkronu). e-MM
/// üretim kararı için müstahsilin <c>KeepsRecords</c> bilgisini sağlar (BK-4). Tüm sorgular tenant
/// global query filter'a tabidir (BK-8). Sales.IProducerRateProfileReader deseniyle aynı fikir.
/// </summary>
public interface IProducerTaxProfileReader
{
    /// <summary>
    /// Bir müstahsilin (Party) vergi/kayıt profilini getirir; profil henüz senkronlanmamışsa null
    /// (bu durumda e-MM üretilmez — temkinli, docs BK-4).
    /// </summary>
    Task<ProducerTaxProfile?> GetByProducerAsync(Guid producerPartyId, CancellationToken cancellationToken = default);
}
