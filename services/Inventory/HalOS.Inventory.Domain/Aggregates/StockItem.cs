using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Domain.Enums;
using HalOS.Inventory.Domain.Events;

namespace HalOS.Inventory.Domain.Aggregates;

/// <summary>
/// Stok Kalemi (StockItem) — Stok &amp; Depo bağlamının kök aggregate'i (docs/02 §115). Bir ürünün
/// (<see cref="ProductId"/>) belirli bir DEPODA (<see cref="WarehouseId"/>) o tenant'taki eldeki
/// miktarını, APPEND-ONLY hareket defterinden (<see cref="StockMovement"/>) türeterek tutar. Cari
/// &amp; Finans <c>CurrentAccount</c>/<c>AccountEntry</c> deseniyle birebir (bakiye/kalan = Σ
/// hareket). Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8). Ürün referansı ID ile
/// (servisler arası FK yok — docs/05 §5); (tenant, depo, ürün) başına tek stok kalemi
/// (UNIQUE(tenant_id, warehouse_id, product_id) — docs/06 S2.1 depo lokasyonu).
///
/// Değişmezler (docs/02 §115; docs/03 BK-7):
/// - <c>QuantityOnHand = Σ StockMovement.SignedQuantity</c> (giriş +, çıkış −). Hareketler
///   APPEND-ONLY; düzeltme yeni hareketle yapılır (destructive işlem yok — docs/07 §8).
/// - <b>Kalan negatif olamaz</b> (BK-7): satış çıkışı ve fire mevcut stoğu AŞAMAZ → aşarsa
///   <see cref="Result.Failure"/> döner ve hareket eklenmez.
/// - Her hareket miktarı pozitif olmalıdır (miktar &gt; 0).
///
/// Stok uyarıları (docs/06 S2.1): opsiyonel yeniden-sipariş eşiği (<see cref="ReorderThreshold"/>)
/// tanımlıysa, bir çıkış hareketi (satış/fire) kalanı eşiğe VEYA altına indirdiğinde
/// <see cref="LowStockAlerted"/> domain event'i yayınlanır (eşik geçişi anında; kalan zaten eşiğin
/// altındayken tekrar çıkış olursa yeniden yayınlanmaz). Eşik null ise uyarı üretilmez.
///
/// Idempotency: aynı kaynak (<see cref="StockMovement.Kind"/>, <see cref="StockMovement.RefId"/>)
/// stok kalemi içinde bir kez işlenir; consumer tekrar tetiklenirse çift hareket oluşmaz (docs/04 §5).
/// </summary>
public sealed class StockItem : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<StockMovement> _movements = new();

    private StockItem(Guid id, Guid tenantId, Guid warehouseId, Guid productId)
        : base(id)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        ProductId = productId;
    }

    /// <summary>ORM materialization only.</summary>
    private StockItem()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>
    /// Stok kaleminin bulunduğu depo (Warehouse ID — docs/06 S2.1 depo lokasyonu). (tenant, depo,
    /// ürün) başına tek kalem. Olay-güdümlü giriş/çıkış varsayılan depoya yazılır (docs/06 S2.1 notu).
    /// </summary>
    public Guid WarehouseId { get; private set; }

    /// <summary>Stoğu tutulan ürün (Product ID — FK değil, docs/05 §5). Depo içinde tekil.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Yeniden-sipariş eşiği (docs/06 S2.1 stok uyarıları); NUMERIC(18,3) nullable (decimal — BK-2).
    /// Kalan bu eşiğe veya altına indiğinde <see cref="LowStockAlerted"/> yayınlanır. Null ise uyarı yok.
    /// </summary>
    public decimal? ReorderThreshold { get; private set; }

    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    /// <summary>
    /// Eldeki miktar = Σ hareket (giriş +, çıkış −), miktar birimine (NUMERIC(18,3)) yuvarlı
    /// (docs/02 §115 değişmez). Türetilmiş değer; kalıcı kolon değildir.
    /// </summary>
    public decimal QuantityOnHand => Math.Round(_movements.Sum(m => m.SignedQuantity), 3, MidpointRounding.AwayFromZero);

    /// <summary>Yeni (boş) bir stok kalemi açar. Depo ve ürün referansı zorunlu.</summary>
    public static Result<StockItem> Open(Guid tenantId, Guid warehouseId, Guid productId)
    {
        if (warehouseId == Guid.Empty)
        {
            return Result.Failure<StockItem>(StockItemErrors.WarehouseRequired);
        }

        if (productId == Guid.Empty)
        {
            return Result.Failure<StockItem>(StockItemErrors.ProductRequired);
        }

        return new StockItem(Guid.NewGuid(), tenantId, warehouseId, productId);
    }

    /// <summary>
    /// Yeniden-sipariş eşiğini ayarlar veya kaldırır (docs/06 S2.1 stok uyarıları). Negatif eşik
    /// kabul edilmez; null verilirse uyarı devre dışı kalır. Bu işlem hareket üretmez.
    /// </summary>
    public Result SetReorderThreshold(decimal? threshold)
    {
        if (threshold is < 0m)
        {
            return Result.Failure(StockItemErrors.NegativeReorderThreshold);
        }

        ReorderThreshold = threshold;
        return Result.Success();
    }

    /// <summary>
    /// Mal girişini stoğa işler (docs/02 §229 ConsignmentReceived → Stok). Kalanı ARTIRIR (+).
    /// Idempotency: aynı <paramref name="consignmentItemId"/> için giriş zaten işlenmişse hareket
    /// eklenmez (çift-kayıt koruması — docs/04 §5). Miktar pozitif olmalıdır.
    /// </summary>
    public Result RecordIntake(Guid consignmentItemId, decimal quantity, DateTime occurredAt)
    {
        if (quantity <= 0m)
        {
            return Result.Failure(StockItemErrors.NonPositiveQuantity);
        }

        if (IsAlreadyRecorded(StockMovementKind.Intake, consignmentItemId))
        {
            // En-az-bir-kez teslimatta consumer yeniden tetiklenebilir; sessizce yut (idempotent).
            return Result.Success();
        }

        Append(StockMovementKind.Intake, Round(quantity), consignmentItemId, reason: null, occurredAt);
        return Result.Success();
    }

    /// <summary>
    /// Satış çıkışını stoktan düşer (docs/02 §230 SaleCompleted → Stok). Kalanı AZALTIR (−).
    /// BK-7: çıkış mevcut stoğu AŞAMAZ (kalan negatif olamaz) → aşarsa <see cref="Result.Failure"/>.
    /// Idempotency: aynı <paramref name="saleLineId"/> için çıkış zaten işlenmişse hareket eklenmez.
    /// Miktar pozitif olmalıdır.
    /// </summary>
    public Result RecordSaleOut(Guid saleLineId, decimal quantity, DateTime occurredAt)
    {
        if (quantity <= 0m)
        {
            return Result.Failure(StockItemErrors.NonPositiveQuantity);
        }

        if (IsAlreadyRecorded(StockMovementKind.SaleOut, saleLineId))
        {
            return Result.Success();
        }

        var rounded = Round(quantity);
        var quantityBefore = QuantityOnHand;
        if (rounded > quantityBefore)
        {
            // BK-7: stok çıkışı mevcut stoğu aşamaz (kalan negatif olamaz).
            return Result.Failure(StockItemErrors.InsufficientStock);
        }

        Append(StockMovementKind.SaleOut, -rounded, saleLineId, reason: null, occurredAt);

        RaiseLowStockAlertIfCrossed(quantityBefore, occurredAt);

        return Result.Success();
    }

    /// <summary>
    /// Fire (zayiat) kaydeder (docs/02 §57 Fire=Spoilage; §237 SpoilageRecorded). Kalanı AZALTIR (−)
    /// ve <see cref="SpoilageRecorded"/> event'ini yayınlar (Finans/AI — docs/02 §237). BK-7: fire
    /// mevcut stoğu AŞAMAZ (kalan negatif olamaz). Miktar pozitif, gerekçe zorunlu.
    /// </summary>
    public Result RecordSpoilage(decimal quantity, string reason, DateTime occurredAt)
    {
        if (quantity <= 0m)
        {
            return Result.Failure(StockItemErrors.NonPositiveQuantity);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(StockItemErrors.SpoilageReasonRequired);
        }

        var rounded = Round(quantity);
        var quantityBefore = QuantityOnHand;
        if (rounded > quantityBefore)
        {
            // BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz).
            return Result.Failure(StockItemErrors.InsufficientStock);
        }

        Append(StockMovementKind.Spoilage, -rounded, refId: null, reason, occurredAt);

        RaiseDomainEvent(new SpoilageRecorded(Id, TenantId, ProductId, rounded, reason, DateTime.UtcNow));

        RaiseLowStockAlertIfCrossed(quantityBefore, occurredAt);

        return Result.Success();
    }

    /// <summary>Bu tür + kaynak referansı zaten işlenmiş mi (idempotency — docs/04 §5).</summary>
    public bool IsAlreadyRecorded(StockMovementKind kind, Guid refId) =>
        _movements.Any(m => m.Kind == kind && m.RefId == refId);

    /// <summary>
    /// Bir çıkış hareketi (satış/fire) sonrası kalan yeniden-sipariş eşiğine VEYA altına indiyse
    /// <see cref="LowStockAlerted"/> yayınlar (docs/06 S2.1). Yalnızca EŞİK GEÇİŞİNDE yayınlanır:
    /// hareketten önce kalan eşiğin ÜSTÜNDEYKEN, hareketten sonra eşiğe/altına inmişse. Kalan zaten
    /// eşiğin altındayken (önceki hareketle uyarı verilmişken) yeniden yayınlanmaz — gürültü önlenir.
    /// Eşik null ise hiçbir uyarı üretilmez.
    /// </summary>
    private void RaiseLowStockAlertIfCrossed(decimal quantityBefore, DateTime occurredAt)
    {
        if (ReorderThreshold is not { } threshold)
        {
            return;
        }

        var quantityAfter = QuantityOnHand;

        // Eşik geçişi: önce eşiğin üstündeydi, şimdi eşikte veya altında.
        if (quantityBefore > threshold && quantityAfter <= threshold)
        {
            RaiseDomainEvent(new LowStockAlerted(
                Id, TenantId, WarehouseId, ProductId, quantityAfter, threshold, occurredAt));
        }
    }

    /// <summary>Aggregate içi ortak hareket ekleme yardımcısı (kapsülleme _movements ile korunur).</summary>
    private void Append(StockMovementKind kind, decimal signedQuantity, Guid? refId, string? reason, DateTime occurredAt)
    {
        var movement = StockMovement.Create(Id, TenantId, kind, signedQuantity, refId, reason, occurredAt);
        _movements.Add(movement);
    }

    private static decimal Round(decimal quantity) => Math.Round(quantity, 3, MidpointRounding.AwayFromZero);
}

/// <summary>Stok kalemi domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
public static class StockItemErrors
{
    public static readonly Error ProductRequired =
        new("StockItem.ProductRequired", "Stok kalemi için ürün referansı zorunludur.");

    public static readonly Error WarehouseRequired =
        new("StockItem.WarehouseRequired", "Stok kalemi için depo referansı zorunludur.");

    public static readonly Error NegativeReorderThreshold =
        new("StockItem.NegativeReorderThreshold", "Yeniden-sipariş eşiği negatif olamaz.");

    public static readonly Error NonPositiveQuantity =
        new("StockItem.NonPositiveQuantity", "Hareket miktarı sıfırdan büyük olmalıdır.");

    public static readonly Error InsufficientStock =
        new("StockItem.InsufficientStock",
            "Stok çıkışı/fire mevcut stok miktarını aşamaz; kalan negatif olamaz (BK-7).");

    public static readonly Error SpoilageReasonRequired =
        new("StockItem.SpoilageReasonRequired", "Fire kaydı için gerekçe zorunludur.");

    public static readonly Error NotFound =
        new("StockItem.NotFound", "Stok kalemi bulunamadı.");
}
