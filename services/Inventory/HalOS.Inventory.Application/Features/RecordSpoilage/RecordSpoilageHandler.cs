using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.RecordSpoilage;

/// <summary>
/// Fire (zayiat) kaydeden handler (docs/03 M9 / BK-7). Ürünün stok kalemini bulur, domain
/// <c>RecordSpoilage</c> ile fire çıkış hareketi işler (BK-7 mevcut stoğu aşma kontrolü domain'de),
/// SaveChanges ile SpoilageRecorded event'i outbox'a atomik yazılır (docs/02 §237; docs/04 §10).
/// Handler doğrudan yayın yapmaz (docs/07 §5). Yutulan Result yok. Finance.RecordCollectionHandler
/// deseniyle birebir.
///
/// Not: Fire için stok kalemi ZORUNLU var olmalıdır — hiç girişi olmayan ürüne fire kaydedilemez
/// (kalan 0 → BK-7 gereği zaten reddedilir). Bu yüzden yoksa hata döner (açılmaz).
/// </summary>
internal sealed class RecordSpoilageHandler : ICommandHandler<RecordSpoilageCommand, Guid>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IUnitOfWork _unitOfWork;

    public RecordSpoilageHandler(
        IStockItemRepository stockItems,
        IUnitOfWork unitOfWork)
    {
        _stockItems = stockItems;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RecordSpoilageCommand request, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItems.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<Guid>(StockItemErrors.NotFound);
        }

        var result = stockItem.RecordSpoilage(request.Quantity, request.Reason, request.OccurredAt);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _stockItems.Update(stockItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stockItem.Id;
    }
}
