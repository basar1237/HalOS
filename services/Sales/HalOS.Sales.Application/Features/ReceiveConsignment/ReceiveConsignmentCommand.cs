using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Features.ReceiveConsignment;

/// <summary>Mal geliş kalemi girdi modeli (docs/02 §3.2).</summary>
public sealed record ConsignmentItemInput(Guid ProductId, decimal Quantity, UnitOfMeasure Unit);

/// <summary>
/// Müstahsilden bir mal geliş partisi kabul eder (docs/03 M3). En az bir kalem zorunlu; her
/// kalem miktarı &gt; 0 (docs/07 §5 validasyon + domain değişmezi).
/// </summary>
public sealed record ReceiveConsignmentCommand(
    Guid ProducerPartyId,
    DateTime ReceivedAt,
    string? DispatchNoteRef,
    IReadOnlyList<ConsignmentItemInput> Items) : ICommand<Guid>;
