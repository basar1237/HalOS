using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.Events;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Mal Geliş (Consignment) aggregate kökü (docs/02 §1.4, §3.2; docs/05 §3.4). Müstahsil/tüccardan
/// gelen mal partisinin kabulünü ve gelen kalemleri tutar. Tenant'a bağlıdır (ITenantOwned →
/// global query filter, docs/07 §6 / BK-8). Müstahsil referansı ID ile (servisler arası FK yok —
/// docs/05 §5).
///
/// Değişmezler:
/// - En az bir kalem içermelidir.
/// - Her kalemin miktarı &gt; 0 olmalıdır.
/// Event: <see cref="ConsignmentReceived"/> (docs/02 §6).
/// </summary>
public sealed class Consignment : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ConsignmentItem> _items = new();

    private Consignment(
        Guid id,
        Guid tenantId,
        Guid producerPartyId,
        DateTime receivedAt,
        string? dispatchNoteRef,
        Guid createdBy,
        DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        ProducerPartyId = producerPartyId;
        ReceivedAt = receivedAt;
        DispatchNoteRef = dispatchNoteRef;
        Status = ConsignmentStatus.Received;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    /// <summary>ORM materialization only.</summary>
    private Consignment()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Malı gönderen müstahsil/tüccar referansı (Party ID — FK değil, docs/05 §5).</summary>
    public Guid ProducerPartyId { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    /// <summary>Sevk irsaliyesi / e-İrsaliye referansı (docs/05 §3.4 <c>dispatch_note_ref</c>).</summary>
    public string? DispatchNoteRef { get; private set; }

    public ConsignmentStatus Status { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<ConsignmentItem> Items => _items.AsReadOnly();

    /// <summary>Girdi kalemi modeli (ürün, miktar, birim).</summary>
    public readonly record struct ItemInput(Guid ProductId, decimal Quantity, UnitOfMeasure Unit);

    /// <summary>
    /// Yeni bir mal geliş partisi kabul eder (docs/03 M3). En az bir kalem zorunlu; her kalem
    /// miktarı &gt; 0 olmalıdır. <see cref="ConsignmentReceived"/> event'i yayınlanır.
    /// </summary>
    public static Result<Consignment> Receive(
        Guid tenantId,
        Guid producerPartyId,
        DateTime receivedAt,
        string? dispatchNoteRef,
        Guid createdBy,
        IReadOnlyCollection<ItemInput> items)
    {
        if (producerPartyId == Guid.Empty)
        {
            return Result.Failure<Consignment>(ConsignmentErrors.ProducerRequired);
        }

        if (items is null || items.Count == 0)
        {
            return Result.Failure<Consignment>(ConsignmentErrors.ItemRequired);
        }

        if (items.Any(i => i.Quantity <= 0m))
        {
            return Result.Failure<Consignment>(ConsignmentErrors.InvalidQuantity);
        }

        if (items.Any(i => i.ProductId == Guid.Empty))
        {
            return Result.Failure<Consignment>(ConsignmentErrors.ProductRequired);
        }

        var consignment = new Consignment(
            Guid.NewGuid(),
            tenantId,
            producerPartyId,
            receivedAt,
            Normalize(dispatchNoteRef),
            createdBy,
            DateTime.UtcNow);

        foreach (var item in items)
        {
            consignment._items.Add(
                ConsignmentItem.Create(consignment.Id, tenantId, item.ProductId, item.Quantity, item.Unit));
        }

        consignment.RaiseDomainEvent(
            new ConsignmentReceived(
                consignment.Id, tenantId, producerPartyId, receivedAt, consignment.CreatedOnUtc));

        return consignment;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public static class ConsignmentErrors
{
    public static readonly Error ProducerRequired =
        new("Consignment.ProducerRequired", "Müstahsil (üretici) referansı zorunludur.");

    public static readonly Error ItemRequired =
        new("Consignment.ItemRequired", "Mal geliş en az bir kalem içermelidir.");

    public static readonly Error InvalidQuantity =
        new("Consignment.InvalidQuantity", "Kalem miktarı sıfırdan büyük olmalıdır.");

    public static readonly Error ProductRequired =
        new("Consignment.ProductRequired", "Kalem için ürün referansı zorunludur.");

    public static readonly Error NotFound =
        new("Consignment.NotFound", "Mal geliş kaydı bulunamadı.");
}
