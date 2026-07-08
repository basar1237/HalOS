using HalOS.BuildingBlocks.Contracts;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Inventory.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="ConsignmentReceived"/>'i tüketip mal geliş partisinin HER kalemi
/// için ilgili ürünün stoğuna GİRİŞ hareketi yazar (docs/02 §115 Stok &amp; Depo; §229 event katalog:
/// ConsignmentReceived → Stok). Stok/kalan = Σ hareket değişmezini korur.
///
/// <b>Idempotency</b> (docs/04 §5): kalem başına (<c>ConsignmentItemId</c>) en fazla bir giriş; aynı
/// event tekrar gelse (broker retry) çift stok girişi oluşmaz — koruma domain
/// <see cref="StockItem.RecordIntake"/> içindedir. İlgili ürünün stok kalemi yoksa açılır (upsert;
/// tenant + ürün başına tek — UNIQUE(tenant_id, product_id)).
///
/// <b>Tenant</b>: broker mesajında HTTP/JWT bağlamı olmadığından tenant, event'in kendisiyle
/// (<see cref="ITenantScopedEvent"/>) taşınır ve <c>TenantConsumeFilter</c> ile ambient tenant'a
/// set edilir; repository/DbContext global query filter'ı DOĞRU tenant'ta çalışır (docs/07 §6 / BK-8).
/// El-yapımı outbox korunur. <b>Yutulan Result yok</b>: domain IsFailure → SaveChanges'ten ÖNCE
/// istisna → MassTransit retry/error queue (docs/04 §10). Consumer içinde HTTP/dış sorgu yok (docs/07 §5).
/// Finance SaleCompletedConsumer deseniyle birebir.
/// </summary>
public sealed class ConsignmentReceivedConsumer : IConsumer<ConsignmentReceived>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsignmentReceivedConsumer> _logger;

    public ConsignmentReceivedConsumer(
        IStockItemRepository stockItems,
        IUnitOfWork unitOfWork,
        ILogger<ConsignmentReceivedConsumer> logger)
    {
        _stockItems = stockItems;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConsignmentReceived> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // Bu Consume çağrısında açılan/getirilen stok kalemlerini ürün bazında izler; aynı ürün
        // birden çok kalemde geçerse ikinci kez yeni kalem açılıp UNIQUE(tenant_id, product_id)
        // ihlali oluşmasın (Finance GetOrOpen deseniyle birebir).
        var openedItems = new Dictionary<Guid, StockItem>();

        foreach (var item in message.Items)
        {
            var stockItem = await GetOrOpenAsync(openedItems, message.TenantId, item.ProductId, ct);

            var result = stockItem.RecordIntake(item.ConsignmentItemId, item.Quantity, message.ReceivedAt);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Stok girişi reddedildi: Tenant={TenantId} Consignment={ConsignmentId} Item={ConsignmentItemId} " +
                    "Ürün={ProductId} Hata={ErrorCode} — {ErrorMessage}.",
                    message.TenantId,
                    message.ConsignmentId,
                    item.ConsignmentItemId,
                    item.ProductId,
                    result.Error.Code,
                    result.Error.Message);

                throw new InvalidOperationException(
                    $"Stok girişi reddedildi (Consignment={message.ConsignmentId}, Item={item.ConsignmentItemId}): {result.Error}");
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Mal girişi stoğa işlendi: Tenant={TenantId} Consignment={ConsignmentId} Kalem={ItemCount}.",
            message.TenantId,
            message.ConsignmentId,
            message.Items.Count);
    }

    /// <summary>
    /// Ürünün stok kalemini getirir; yoksa açar ve repository'ye ekler (upsert). Aynı Consume
    /// çağrısında zaten açılmış/getirilmiş bir kalem varsa onu yeniden kullanır — yeni satır açıp
    /// UNIQUE(tenant_id, product_id) ihlaline yol açmaz.
    /// </summary>
    private async Task<StockItem> GetOrOpenAsync(
        Dictionary<Guid, StockItem> openedItems,
        Guid tenantId,
        Guid productId,
        CancellationToken ct)
    {
        if (openedItems.TryGetValue(productId, out var tracked))
        {
            return tracked;
        }

        var stockItem = await _stockItems.GetByProductIdAsync(productId, ct);
        if (stockItem is null)
        {
            // Open, tenant'ı parametre alır; ambient tenant SaveChanges'te de aynı değeri uygular (BK-8).
            stockItem = StockItem.Open(tenantId, productId).Value;
            _stockItems.Add(stockItem);
        }

        openedItems[productId] = stockItem;
        return stockItem;
    }
}
