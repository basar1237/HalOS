using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Domain.Aggregates;

/// <summary>
/// Tek bir stok hareketi (docs/02 §115 Stok &amp; Depo bağlamı). APPEND-ONLY: hareketler
/// silinmez/değiştirilmez, düzeltme ters/yeni kayıtla yapılır (Cari &amp; Finans <c>AccountEntry</c>
/// deseniyle birebir). Kalan (QuantityOnHand) bu hareketlerden türetilir; <see cref="Kind"/>
/// hareket türünü, <see cref="SignedQuantity"/> ise kalana katkıyı (giriş +, çıkış −) taşır. Bir
/// <see cref="StockItem"/>'ın bağlı entity'sidir. Miktar NUMERIC(18,3) (decimal — asla float, BK-2).
///
/// <see cref="RefId"/> kaynak referansıdır (consignment_item_id / sale_line_id — servisler arası FK
/// YOK, docs/05 §5). Idempotency: aynı stok kalemi içinde (<see cref="Kind"/>, <see cref="RefId"/>)
/// tekildir; aynı event tekrar tüketilse (broker retry) çift hareket oluşmaz (docs/04 §5).
/// </summary>
public sealed class StockMovement : Entity<Guid>, ITenantOwned
{
    private StockMovement(
        Guid id,
        Guid stockItemId,
        Guid tenantId,
        StockMovementKind kind,
        decimal signedQuantity,
        Guid? refId,
        string? reason,
        DateTime occurredAt)
        : base(id)
    {
        StockItemId = stockItemId;
        TenantId = tenantId;
        Kind = kind;
        SignedQuantity = signedQuantity;
        RefId = refId;
        Reason = reason;
        OccurredAt = occurredAt;
    }

    /// <summary>ORM materialization only.</summary>
    private StockMovement()
    {
    }

    public Guid StockItemId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Hareket türü (intake/sale-out/spoilage/adjustment) — metin kolon (docs/07).</summary>
    public StockMovementKind Kind { get; private set; }

    /// <summary>
    /// Kalana işaretli katkı: giriş pozitif, çıkış (satış/fire) negatif. Kalan = Σ
    /// <see cref="SignedQuantity"/> (docs/02 §115 değişmez). Miktar NUMERIC(18,3) (BK-2).
    /// </summary>
    public decimal SignedQuantity { get; private set; }

    /// <summary>
    /// Kaynak referansı: giriş için <c>consignment_item_id</c>, satış çıkışı için <c>sale_line_id</c>
    /// (docs/05 §5 servisler arası FK yok). Fire/düzeltmede null olabilir. (Kind, RefId) idempotency
    /// anahtarının parçasıdır.
    /// </summary>
    public Guid? RefId { get; private set; }

    /// <summary>Fire/düzeltme gerekçesi (opsiyonel; fire kaydında dolar — docs/02 §57).</summary>
    public string? Reason { get; private set; }

    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Yeni bir stok hareketi üretir (aggregate içinden çağrılır). İşaretli miktar zaten yön bilgisini
    /// taşır (giriş +, çıkış −); miktar/işaret doğrulaması <see cref="StockItem"/>'da yapılır.
    /// </summary>
    internal static StockMovement Create(
        Guid stockItemId,
        Guid tenantId,
        StockMovementKind kind,
        decimal signedQuantity,
        Guid? refId,
        string? reason,
        DateTime occurredAt) =>
        new(Guid.NewGuid(), stockItemId, tenantId, kind, signedQuantity, refId, reason, occurredAt);
}
