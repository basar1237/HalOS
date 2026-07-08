using HalOS.BuildingBlocks.Contracts;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Inventory.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'i tüketip satışın HER satırı için ilgili
/// ürünün stoğundan ÇIKIŞ hareketi yazar (docs/02 §115 Stok &amp; Depo; §230 event katalog:
/// SaleCompleted → Stok). Stok/kalan = Σ hareket değişmezini korur. Finance/Integration
/// consumer'ları toplam/net tutarlarla çalışır; Inventory <see cref="SaleCompleted.Lines"/>
/// kırılımını kullanır.
///
/// <b>Idempotency</b> (docs/04 §5): satır başına (<c>SaleLineId</c>) en fazla bir çıkış; aynı satış
/// tekrar gelse (broker retry) çift çıkış oluşmaz — koruma domain
/// <see cref="StockItem.RecordSaleOut"/> içindedir. İlgili ürünün stok kalemi yoksa açılır (upsert).
/// <b>BK-7</b>: çıkış mevcut stoğu aşarsa domain <see cref="StockItem.RecordSaleOut"/> Result.Failure
/// döndürür → bu consumer istisna fırlatır (yutulan Result yok) ve HİÇBİR satır kalıcılaşmaz →
/// MassTransit retry/error queue (docs/04 §10).
///
/// <b>Tenant</b>: event'in kendisiyle (<see cref="ITenantScopedEvent"/>) taşınır ve
/// <c>TenantConsumeFilter</c> ile ambient tenant'a set edilir (docs/07 §6 / BK-8). El-yapımı outbox
/// korunur. Consumer içinde HTTP/dış sorgu yok (docs/07 §5).
/// </summary>
public sealed class SaleCompletedConsumer : IConsumer<SaleCompleted>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(
        IStockItemRepository stockItems,
        IUnitOfWork unitOfWork,
        ILogger<SaleCompletedConsumer> logger)
    {
        _stockItems = stockItems;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // Aynı ürün birden çok satırda geçerse tek stok kalemi kullanılır (UNIQUE(tenant_id,
        // product_id) ihlalini önler); ayrıca aynı kalemde biriken çıkışlar BK-7 kontrolünü
        // (kalan negatif olamaz) doğru şekilde kümülatif değerlendirir.
        var openedItems = new Dictionary<Guid, StockItem>();

        foreach (var line in message.Lines)
        {
            var stockItem = await GetOrOpenAsync(openedItems, message.TenantId, line.ProductId, ct);

            var result = stockItem.RecordSaleOut(line.SaleLineId, line.Quantity, message.SoldAt);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Stok çıkışı reddedildi: Tenant={TenantId} Sale={SaleTransactionId} SaleLine={SaleLineId} " +
                    "Ürün={ProductId} Hata={ErrorCode} — {ErrorMessage}.",
                    message.TenantId,
                    message.SaleTransactionId,
                    line.SaleLineId,
                    line.ProductId,
                    result.Error.Code,
                    result.Error.Message);

                throw new InvalidOperationException(
                    $"Stok çıkışı reddedildi (Sale={message.SaleTransactionId}, SaleLine={line.SaleLineId}): {result.Error}");
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Satış stoktan düşüldü: Tenant={TenantId} Sale={SaleTransactionId} Satır={LineCount}.",
            message.TenantId,
            message.SaleTransactionId,
            message.Lines.Count);
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
            stockItem = StockItem.Open(tenantId, productId).Value;
            _stockItems.Add(stockItem);
        }

        openedItems[productId] = stockItem;
        return stockItem;
    }
}
