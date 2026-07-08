namespace HalOS.Inventory.Domain.Enums;

/// <summary>
/// Stok hareketinin türü (docs/02 §115 Stok &amp; Depo bağlamı; §229-230 event katalog). Stok/kalan
/// = Σ hareket değişmezinde her tür belirli bir yönle (giriş +, çıkış −) katkı verir
/// (bkz. <see cref="Aggregates.StockMovement.SignedQuantity"/>). Cari &amp; Finans'taki
/// <c>EntryType</c> deseniyle birebir. Enum kolonu metin olarak saklanır (HasConversion&lt;string&gt;
/// — docs/07).
/// </summary>
public enum StockMovementKind
{
    /// <summary>Mal girişi (ConsignmentReceived → stok girişi) — kalanı ARTIRIR (+) (docs/02 §229).</summary>
    Intake = 1,

    /// <summary>Satış çıkışı (SaleCompleted → stok çıkışı) — kalanı AZALTIR (−) (docs/02 §230).</summary>
    SaleOut = 2,

    /// <summary>Fire/zayiat (docs/02 §57 Fire=Spoilage; §237 SpoilageRecorded) — kalanı AZALTIR (−).</summary>
    Spoilage = 3,

    /// <summary>Manuel düzeltme (sayım farkı vb.) — işaret miktarın yönüne göre değişir.</summary>
    Adjustment = 4
}
