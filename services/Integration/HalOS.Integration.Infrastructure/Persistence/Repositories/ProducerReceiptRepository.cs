using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// ProducerReceipt (e-MM) aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Kesinti kalemleri (ReceiptDeduction) aggregate ile birlikte yüklenir (belge
/// bütünlüğü için). Finance.CurrentAccountRepository deseniyle birebir. Idempotency ön-kontrolü
/// (<see cref="GetBySaleTransactionIdAsync"/>) + DB UNIQUE(tenant_id, sale_transaction_id) birlikte;
/// eşzamanlı teslimatta ikinci SaveChanges DbUpdateException verir, MassTransit retry'ında ön-kontrol
/// mevcut belgeyi bulup atlar (idempotent).
/// </summary>
internal sealed class ProducerReceiptRepository : IProducerReceiptRepository
{
    private readonly IntegrationDbContext _dbContext;

    public ProducerReceiptRepository(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProducerReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ProducerReceipts
            .Include(r => r.Deductions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<ProducerReceipt?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default) =>
        _dbContext.ProducerReceipts
            .Include(r => r.Deductions)
            .FirstOrDefaultAsync(r => r.SaleTransactionId == saleTransactionId, cancellationToken);

    public async Task<PagedResult<ProducerReceipt>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProducerReceipts
            .AsNoTracking()
            .Include(r => r.Deductions)
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProducerReceipt>(items, page, pageSize, totalCount);
    }

    public void Add(ProducerReceipt receipt) => _dbContext.ProducerReceipts.Add(receipt);

    public void Update(ProducerReceipt receipt) => _dbContext.ProducerReceipts.Update(receipt);
}
