using HalOS.Integration.Domain.ReadModels;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// Müstahsil vergi/kayıt profili okuma modelinin upsert (yaz) port'u — Party senkron consumer'ı
/// kullanır (docs/02 §6). Getirilen satır DbContext tarafından izlenir; <c>Apply</c> ile yerinde
/// güncellenir ve <see cref="IUnitOfWork.SaveChangesAsync"/> ile kalıcılaşır (EF change tracking).
/// Tüm sorgular tenant global query filter'a tabidir (BK-8). Okuma-yolu (<see cref="IProducerTaxProfileReader"/>)
/// AsNoTracking olduğundan upsert için ayrı, izlemeli bu port kullanılır.
/// </summary>
public interface IProducerTaxProfileWriter
{
    /// <summary>Bir müstahsilin profilini İZLENEN olarak getirir (upsert güncelleme kolu için); yoksa null.</summary>
    Task<ProducerTaxProfile?> GetByProducerAsync(Guid producerPartyId, CancellationToken cancellationToken = default);

    /// <summary>Yeni profil satırını ekler (upsert ekleme kolu).</summary>
    void Add(ProducerTaxProfile profile);
}
