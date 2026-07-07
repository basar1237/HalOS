using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Müstahsil vergi/kayıt profili OKUMA adaptörü (AsNoTracking) — <see cref="IProducerTaxProfileReader"/>.
/// SaleCompletedConsumer e-MM kararı için müstahsilin KeepsRecords bilgisini buradan okur (BK-4).
/// Tenant global query filter'a tabidir (BK-8). Yazma yolu ayrı, izlemeli
/// <see cref="ProducerTaxProfileWriter"/>'dadır.
/// </summary>
internal sealed class ProducerTaxProfileReader : IProducerTaxProfileReader
{
    private readonly IntegrationDbContext _dbContext;

    public ProducerTaxProfileReader(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProducerTaxProfile?> GetByProducerAsync(Guid producerPartyId, CancellationToken cancellationToken = default) =>
        _dbContext.ProducerTaxProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProducerPartyId == producerPartyId, cancellationToken);
}
