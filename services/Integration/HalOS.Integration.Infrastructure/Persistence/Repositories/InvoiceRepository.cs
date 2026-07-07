using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Invoice (e-Fatura HAL) aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Idempotency ön-kontrolü (<see cref="GetBySaleTransactionIdAsync"/>) + DB
/// UNIQUE(tenant_id, sale_transaction_id) birlikte. ProducerReceiptRepository deseniyle birebir.
/// </summary>
internal sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly IntegrationDbContext _dbContext;

    public InvoiceRepository(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Invoice?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default) =>
        _dbContext.Invoices
            .FirstOrDefaultAsync(i => i.SaleTransactionId == saleTransactionId, cancellationToken);

    public async Task<PagedResult<Invoice>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Invoices
            .AsNoTracking()
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice>(items, page, pageSize, totalCount);
    }

    public void Add(Invoice invoice) => _dbContext.Invoices.Add(invoice);

    public void Update(Invoice invoice) => _dbContext.Invoices.Update(invoice);
}
