using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Abstractions;

/// <summary>
/// SaleTransaction aggregate persistence port'u. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Satış satırları, komisyon/kesinti/hakediş bağlı entity'leriyle birlikte yüklenir.
/// </summary>
public interface ISaleTransactionRepository
{
    /// <summary>Satışı satırları/kesinti/hakedişiyle birlikte getirir.</summary>
    Task<SaleTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Offline idempotency (docs/04 §5): verilen operationId ile satış zaten var mı. Aynı
    /// operationId ile ikinci CreateSale reddedilir/geri döner.
    /// </summary>
    Task<SaleTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant filtreli, sold_at'e göre azalan sıralı sayfalanmış satış listesi (docs/05 §6
    /// (tenant_id, sold_at) indeksi). Opsiyonel tarih aralığı filtresi.
    /// </summary>
    Task<PagedResult<SaleTransaction>> ListAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    void Add(SaleTransaction sale);

    void Update(SaleTransaction sale);

    /// <summary>
    /// İzlenen (tracked) bir aggregate'e domain metoduyla EKLENEN yeni bağlı entity'yi (satır,
    /// kesinti, komisyon, hakediş) EF'e açıkça "Added" olarak bildirir. Gerekli, çünkü bu bağlı
    /// entity'lerin birincil anahtarı client-generated (Guid) olduğundan EF, navigasyondan
    /// keşfettiği dolu-anahtarlı yeni çocuğu yanlışlıkla "Modified" (UPDATE → 0 satır → hata)
    /// sayar. Yeni aggregate'ler için <see cref="Add"/> kullanılır; bu yalnız mevcut aggregate'e
    /// çocuk eklerken gereklidir.
    /// </summary>
    void RegisterNew(object child);

    /// <summary>
    /// Satış özet raporu (docs/03 M10 "raporlar (okuma)"). Verilen [from, to] aralığındaki (SoldAt)
    /// TAMAMLANMIŞ (Completed) satışların adet/brüt/komisyon/kesinti(KDV hariç)/net toplamları.
    /// AsNoTracking, tenant filtreli (BK-8). Yeni tablo YOK — mevcut veriden agregasyon.
    /// </summary>
    Task<SalesSummaryReportDto> GetSalesSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Komisyon geliri raporu (docs/03 M10). Aralıktaki tamamlanmış satışlar için Σ komisyon +
    /// Σ komisyon KDV'si; <paramref name="includeDailyBreakdown"/> ise SoldAt gününe göre kırılım.
    /// AsNoTracking, tenant filtreli (BK-8).
    /// </summary>
    Task<CommissionIncomeReportDto> GetCommissionIncomeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        bool includeDailyBreakdown,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gün sonu özet raporu (docs/03 M10). Verilen günün (SoldAt tarihi) tamamlanmış satış
    /// toplamları (adet/brüt/komisyon/net). AsNoTracking, tenant filtreli (BK-8).
    /// </summary>
    Task<DailySummaryReportDto> GetDailySummaryAsync(
        DateTime day,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Satış trend raporu (docs/06 S2.2). Verilen [from, to] aralığındaki (SoldAt) TAMAMLANMIŞ
    /// (Completed) satışları <paramref name="granularity"/> seviyesine (Gün/Hafta/Ay) göre zaman
    /// kovalarına gruplar; her kova için adet/brüt/komisyon/net toplamı (PeriodStart artan sıralı).
    /// AsNoTracking, tenant filtreli (BK-8). Yeni tablo YOK — mevcut veriden agregasyon.
    /// </summary>
    Task<SalesTrendReportDto> GetSalesTrendAsync(
        DateTime fromUtc,
        DateTime toUtc,
        TrendGranularity granularity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ödenmemiş müstahsil hakedişi toplamı (dashboard "Bekleyen Hakediş"). Tamamlanmış satışların
    /// Settlement.Status ≠ Paid olan Σ NetAmount'u. AsNoTracking, tenant filtreli (BK-8).
    /// </summary>
    Task<decimal> GetPendingSettlementTotalAsync(CancellationToken cancellationToken = default);
}
