using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Abstractions;

/// <summary>Consignment aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir (BK-8).</summary>
public interface IConsignmentRepository
{
    Task<Consignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Verilen günde (ReceivedAt tarihi, UTC) kabul edilen mal geliş partisi adedi (tenant filtreli).</summary>
    Task<long> CountReceivedOnAsync(DateTime day, CancellationToken cancellationToken = default);

    void Add(Consignment consignment);
}
