namespace HalOS.Inventory.Application.Contracts;

/// <summary>
/// Detaylı fire analizi raporu okuma DTO'su (docs/06 S2.1 detaylı fire analizi). Verilen aralıkta
/// (<paramref name="FromUtc"/>..<paramref name="ToUtc"/>) ÜRÜN BAZLI toplam giriş, toplam fire ve
/// fire oranını (%) döner. StockMovement'lardan Kind-bazlı agregasyon (giriş=Intake, fire=Spoilage);
/// tenant global filter otomatik (BK-8). Yeni tablo/servis YOK (SALT-OKUMA CQRS). Miktarlar decimal
/// (BK-2). Finance yaşlandırma raporu (CurrentAccountAgingReportDto) deseniyle birebir.
/// </summary>
/// <param name="FromUtc">Analiz aralığı başlangıcı (UTC, dahil).</param>
/// <param name="ToUtc">Analiz aralığı bitişi (UTC, dahil).</param>
/// <param name="Items">Ürün bazlı fire analizi satırları (giriş miktarına göre azalan).</param>
public sealed record SpoilageAnalysisReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<SpoilageAnalysisItemDto> Items);

/// <summary>
/// Ürün bazlı fire analizi satırı (docs/06 S2.1). Miktarlar decimal (NUMERIC(18,3); BK-2). Oran
/// yüzdedir (0..100), giriş 0 ise 0 (sıfıra bölme yok).
/// </summary>
/// <param name="ProductId">Ürün referansı (FK değil, docs/05 §5).</param>
/// <param name="TotalIntake">Aralıktaki toplam giriş miktarı (Intake).</param>
/// <param name="TotalSpoilage">Aralıktaki toplam fire miktarı (Spoilage).</param>
/// <param name="SpoilageRatePercent">Fire oranı yüzdesi = fire / giriş * 100 (giriş 0 ise 0).</param>
public sealed record SpoilageAnalysisItemDto(
    Guid ProductId,
    decimal TotalIntake,
    decimal TotalSpoilage,
    decimal SpoilageRatePercent);
