using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Sales.Infrastructure.Persistence.Repositories;

internal sealed class SaleTransactionRepository : ISaleTransactionRepository
{
    private readonly SalesDbContext _dbContext;

    public SaleTransactionRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SaleTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<SaleTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        _dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .FirstOrDefaultAsync(s => s.OperationId == operationId, cancellationToken);

    public async Task<PagedResult<SaleTransaction>> ListAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SaleTransactions
            .AsNoTracking()
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .AsQueryable();

        if (from is not null)
        {
            query = query.Where(s => s.SoldAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(s => s.SoldAt <= to.Value);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.SoldAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleTransaction>(items, page, pageSize, totalCount);
    }

    public async Task<decimal> GetPendingSettlementTotalAsync(CancellationToken cancellationToken = default) =>
        // Tamamlanmış satışların ödenmemiş (Status ≠ Paid) hakediş net toplamı. Tenant filter otomatik (BK-8).
        await _dbContext.SaleTransactions
            .AsNoTracking()
            .Where(s => s.Status == SaleStatus.Completed
                        && s.Settlement != null
                        && s.Settlement.Status != SettlementStatus.Paid)
            .SumAsync(s => (decimal?)s.Settlement!.NetAmount, cancellationToken) ?? 0m;

    public void Add(SaleTransaction sale) => _dbContext.SaleTransactions.Add(sale);

    public void Update(SaleTransaction sale) => _dbContext.SaleTransactions.Update(sale);

    public void RegisterNew(object child) => _dbContext.Entry(child).State = EntityState.Added;

    public async Task<SalesSummaryReportDto> GetSalesSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        // Yalnız tamamlanmış (Completed) satışlar; aralık gün bazında [from, to] DAHİL (üst sınır
        // CompletedInRange içinde ertesi gün 00:00'a normalize edilir); tenant global filter otomatik
        // (BK-8). AsNoTracking rapor okuması. KDV hariç kesinti = Vat dışı kalemler.
        var query = CompletedInRange(fromUtc, toUtc);

        var count = await query.LongCountAsync(cancellationToken);
        var totalGross = await query.SumAsync(s => (decimal?)s.GrossAmount, cancellationToken) ?? 0m;
        var totalCommission = await query
            .Where(s => s.CommissionCalculation != null)
            .SumAsync(s => (decimal?)s.CommissionCalculation!.CommissionAmount, cancellationToken) ?? 0m;
        var totalNet = await query
            .Where(s => s.Settlement != null)
            .SumAsync(s => (decimal?)s.Settlement!.NetAmount, cancellationToken) ?? 0m;

        // KDV hariç toplam kesinti (docs/02 §4, BK-1): komisyon + stopaj + Bağ-Kur + rüsum.
        var totalDeductions = await query
            .SelectMany(s => s.Deductions)
            .Where(d => d.Type != DeductionType.Vat)
            .SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0m;

        return new SalesSummaryReportDto(count, totalGross, totalCommission, totalDeductions, totalNet);
    }

    public async Task<CommissionIncomeReportDto> GetCommissionIncomeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        bool includeDailyBreakdown,
        CancellationToken cancellationToken = default)
    {
        var query = CompletedInRange(fromUtc, toUtc)
            .Where(s => s.CommissionCalculation != null);

        var totalCommission = await query
            .SumAsync(s => (decimal?)s.CommissionCalculation!.CommissionAmount, cancellationToken) ?? 0m;
        var totalVat = await query
            .SumAsync(s => (decimal?)s.CommissionCalculation!.VatAmount, cancellationToken) ?? 0m;

        var daily = new List<CommissionIncomeDailyDto>();
        if (includeDailyBreakdown)
        {
            var grouped = await query
                .GroupBy(s => s.SoldAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Commission = g.Sum(s => s.CommissionCalculation!.CommissionAmount),
                    Vat = g.Sum(s => s.CommissionCalculation!.VatAmount)
                })
                .OrderBy(x => x.Day)
                .ToListAsync(cancellationToken);

            daily = grouped
                .Select(x => new CommissionIncomeDailyDto(x.Day, x.Commission, x.Vat, x.Commission + x.Vat))
                .ToList();
        }

        return new CommissionIncomeReportDto(
            totalCommission,
            totalVat,
            totalCommission + totalVat,
            daily);
    }

    public async Task<DailySummaryReportDto> GetDailySummaryAsync(
        DateTime day,
        CancellationToken cancellationToken = default)
    {
        // Günün [00:00, ertesi gün 00:00) aralığı — SoldAt bu gün içindeki tamamlanmış satışlar.
        var start = day.Date;
        var end = start.AddDays(1);

        var query = _dbContext.SaleTransactions
            .AsNoTracking()
            .Where(s => s.Status == SaleStatus.Completed && s.SoldAt >= start && s.SoldAt < end);

        var count = await query.LongCountAsync(cancellationToken);
        var gross = await query.SumAsync(s => (decimal?)s.GrossAmount, cancellationToken) ?? 0m;
        var commission = await query
            .Where(s => s.CommissionCalculation != null)
            .SumAsync(s => (decimal?)s.CommissionCalculation!.CommissionAmount, cancellationToken) ?? 0m;
        var net = await query
            .Where(s => s.Settlement != null)
            .SumAsync(s => (decimal?)s.Settlement!.NetAmount, cancellationToken) ?? 0m;

        return new DailySummaryReportDto(start, count, gross, commission, net);
    }

    public async Task<SalesTrendReportDto> GetSalesTrendAsync(
        DateTime fromUtc,
        DateTime toUtc,
        TrendGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        // Tamamlanmış satışların yalnız kovalama için gereken alanlarını server-side (tenant filtreli,
        // AsNoTracking) projekte edip çekeriz; kova başlangıcı hesabı (ISO hafta başı / ay başı) SQL'e
        // çevrilemeyeceğinden ve sağlayıcı-bağımsız (InMemory + Postgres) kalması için gruplama
        // BELLEKTE yapılır. Rapor okuması küçük hacimli/tarih-aralığı sınırlıdır (docs/06 S2.2).
        var rows = await CompletedInRange(fromUtc, toUtc)
            .Select(s => new
            {
                s.SoldAt,
                Gross = s.GrossAmount,
                Commission = s.CommissionCalculation != null ? s.CommissionCalculation.CommissionAmount : 0m,
                Net = s.Settlement != null ? s.Settlement.NetAmount : 0m
            })
            .ToListAsync(cancellationToken);

        var buckets = rows
            .GroupBy(r => BucketStart(r.SoldAt, granularity))
            .Select(g => new SalesTrendBucketDto(
                g.Key,
                g.LongCount(),
                g.Sum(r => r.Gross),
                g.Sum(r => r.Commission),
                g.Sum(r => r.Net)))
            .OrderBy(b => b.PeriodStart)
            .ToList();

        return new SalesTrendReportDto(granularity, buckets);
    }

    /// <summary>
    /// Bir satış tarihini (UTC) kova başlangıcına indirger: Gün → gün başı; Hafta → ISO-8601 hafta
    /// başı (Pazartesi 00:00); Ay → ayın ilk günü 00:00. Kültür-bağımsız (haftanın günü ISO'ya göre).
    /// </summary>
    private static DateTime BucketStart(DateTime soldAt, TrendGranularity granularity)
    {
        var day = soldAt.Date;
        return granularity switch
        {
            TrendGranularity.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
            TrendGranularity.Month => new DateTime(day.Year, day.Month, 1, 0, 0, 0, day.Kind),
            _ => day
        };
    }

    /// <summary>
    /// Rapor okumaları için ortak temel: tamamlanmış (Completed) satışlar. Aralık üst sınırı gün
    /// bazında DAHİL'dir (query XML doc "ToUtc (dahil)"): <paramref name="toUtc"/> gün başına
    /// normalize edilip ertesi gün 00:00'a taşınır ve yarı-açık [fromUtc, toUtcExclusive) filtresi
    /// uygulanır. Böylece bitiş gününün TÜM saatleri (ör. to=07.07 → 07.07 10:00 satışı) dahildir;
    /// aksi halde <c>SoldAt &lt;= toUtc</c> (toUtc = gün başı) aynı günün satışlarını sessizce
    /// düşürürdü. Bu semantik <see cref="GetDailySummaryAsync"/> ile tutarlıdır. AsNoTracking;
    /// tenant global filter otomatik uygulanır (BK-8).
    /// </summary>
    private IQueryable<SaleTransaction> CompletedInRange(DateTime fromUtc, DateTime toUtc)
    {
        // Bitiş gününü tümüyle kapsamak için üst sınırı ertesi gün 00:00'a normalize et (dışlayıcı).
        var toUtcExclusive = toUtc.Date.AddDays(1);
        return _dbContext.SaleTransactions
            .AsNoTracking()
            .Where(s => s.Status == SaleStatus.Completed && s.SoldAt >= fromUtc && s.SoldAt < toUtcExclusive);
    }
}
