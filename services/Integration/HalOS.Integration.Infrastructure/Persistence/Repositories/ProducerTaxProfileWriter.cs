using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Müstahsil vergi/kayıt profili YAZMA (upsert) adaptörü — <see cref="IProducerTaxProfileWriter"/>.
/// ProducerWithholdingProfileChangedConsumer kullanır. Getirilen satır İZLENİR; <c>Apply</c> ile
/// yerinde güncellenir ve SaveChanges ile kalıcılaşır (EF change tracking). Tenant global query
/// filter'a tabidir (BK-8). Okuma yolu (<see cref="ProducerTaxProfileReader"/>) AsNoTracking'tir.
/// </summary>
internal sealed class ProducerTaxProfileWriter : IProducerTaxProfileWriter
{
    private readonly IntegrationDbContext _dbContext;

    public ProducerTaxProfileWriter(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProducerTaxProfile?> GetByProducerAsync(Guid producerPartyId, CancellationToken cancellationToken = default) =>
        _dbContext.ProducerTaxProfiles
            .FirstOrDefaultAsync(p => p.ProducerPartyId == producerPartyId, cancellationToken);

    public void Add(ProducerTaxProfile profile) => _dbContext.ProducerTaxProfiles.Add(profile);
}
