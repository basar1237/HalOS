using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Contracts;

/// <summary>Mal geliş kalemi okuma DTO'su (docs/05 §3.4 <c>consignment_item</c>).</summary>
public sealed record ConsignmentItemDto(Guid Id, Guid ProductId, decimal Quantity, UnitOfMeasure Unit);

/// <summary>Mal geliş okuma DTO'su. Domain aggregate'i API'ye sızmaz.</summary>
public sealed record ConsignmentDto(
    Guid Id,
    Guid TenantId,
    Guid ProducerPartyId,
    DateTime ReceivedAt,
    string? DispatchNoteRef,
    ConsignmentStatus Status,
    IReadOnlyList<ConsignmentItemDto> Items)
{
    public static ConsignmentDto FromDomain(Consignment consignment) => new(
        consignment.Id,
        consignment.TenantId,
        consignment.ProducerPartyId,
        consignment.ReceivedAt,
        consignment.DispatchNoteRef,
        consignment.Status,
        consignment.Items
            .Select(i => new ConsignmentItemDto(i.Id, i.ProductId, i.Quantity, i.Unit))
            .ToList());
}
