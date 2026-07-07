using HalOS.Sales.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Sales.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IProducerRateProfileReader"/>'ın EF Core uygulaması. Salt-okunur (AsNoTracking)
/// sorgu tenant global query filter'a tabidir (BK-8) → yalnızca geçerli tenant'ın müstahsili döner.
/// </summary>
internal sealed class ProducerRateProfileReader : IProducerRateProfileReader
{
    private readonly SalesDbContext _dbContext;

    public ProducerRateProfileReader(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProducerRateSnapshot?> FindAsync(
        Guid producerPartyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProducerRateProfiles
            .AsNoTracking()
            .Where(p => p.ProducerPartyId == producerPartyId)
            .Select(p => new ProducerRateSnapshot(p.AgriWithholdingRate, p.FarmerSskRate))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
