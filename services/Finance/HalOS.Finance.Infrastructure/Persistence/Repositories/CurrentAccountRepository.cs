using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Finance.Infrastructure.Persistence.Repositories;

/// <summary>
/// CurrentAccount aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Hareketler (AccountEntry) aggregate ile birlikte yüklenir çünkü bakiye
/// türetilir (docs/02 §3.4) ve iş metotları hareket koleksiyonuna ihtiyaç duyar.
/// </summary>
internal sealed class CurrentAccountRepository : ICurrentAccountRepository
{
    private readonly FinanceDbContext _dbContext;

    public CurrentAccountRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentAccounts
            .Include(a => a.Entries)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<CurrentAccount?> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentAccounts
            .Include(a => a.Entries)
            .FirstOrDefaultAsync(a => a.PartyId == partyId, cancellationToken);

    public async Task<PagedResult<CurrentAccount>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentAccounts
            .AsNoTracking()
            .Include(a => a.Entries)
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.PartyId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CurrentAccount>(items, page, pageSize, totalCount);
    }

    public void Add(CurrentAccount account) => _dbContext.CurrentAccounts.Add(account);

    public void Update(CurrentAccount account) => _dbContext.CurrentAccounts.Update(account);

    public async Task<CurrentAccountAgingReportDto> GetCurrentAccountAgingAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // Yaşlandırma tabanı: yalnızca vade taşıyan müstahsil hakediş (Settlement) hareketleri.
        // AsNoTracking rapor okuması; tenant global filter otomatik uygulanır (BK-8). Alıcı satış
        // borçları (Sale) vade taşımaz (DueDate null) → dahil edilmez.
        var asOfDate = asOfUtc.Date;

        // Kova sınırlarını gün sayısı olarak SQL'e taşınabilir tarih karşılaştırmalarına çevirir:
        // güncel: DueDate >= AsOf; 0-15: AsOf-15 <= DueDate < AsOf; 16-30: AsOf-30 <= DueDate < AsOf-15;
        // 31+: DueDate < AsOf-30. Sınır tarihleri sabit (parametrik) olduğundan provider'da çevrilebilir.
        var current = asOfDate;
        var days15 = asOfDate.AddDays(-15);
        var days30 = asOfDate.AddDays(-30);

        var dueEntries = _dbContext.AccountEntries
            .AsNoTracking()
            .Where(e => e.Type == EntryType.Settlement && e.DueDate != null);

        // Her kova için: Σ tutar + benzersiz cari (CurrentAccountId) sayısı.
        var buckets = await dueEntries
            .GroupBy(e =>
                e.DueDate!.Value >= current ? 0 :             // güncel (vadesi gelmemiş)
                e.DueDate!.Value >= days15 ? 1 :              // 1-15 gün gecikmiş
                e.DueDate!.Value >= days30 ? 2 :              // 16-30 gün gecikmiş
                3)                                            // 31+ gün gecikmiş
            .Select(g => new
            {
                Bucket = g.Key,
                Amount = g.Sum(e => e.Amount),
                AccountCount = g.Select(e => e.CurrentAccountId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        AgingBucketDto Bucket(int key)
        {
            var row = buckets.FirstOrDefault(b => b.Bucket == key);
            return row is null ? new AgingBucketDto(0m, 0) : new AgingBucketDto(row.Amount, row.AccountCount);
        }

        var currentBucket = Bucket(0);
        var b0To15 = Bucket(1);
        var b16To30 = Bucket(2);
        var b31Plus = Bucket(3);

        var totalAmount = currentBucket.Amount + b0To15.Amount + b16To30.Amount + b31Plus.Amount;

        // Toplam benzersiz cari: kovalar arası aynı cari birden fazla kovaya düşebileceğinden
        // kova sayıları toplanamaz; ayrı bir distinct sayım gerekir.
        var totalAccountCount = await dueEntries
            .Select(e => e.CurrentAccountId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new CurrentAccountAgingReportDto(
            asOfUtc,
            currentBucket,
            b0To15,
            b16To30,
            b31Plus,
            totalAmount,
            totalAccountCount);
    }
}
