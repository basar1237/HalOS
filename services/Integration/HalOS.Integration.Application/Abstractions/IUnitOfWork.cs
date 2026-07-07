namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// Değişiklikleri tek transaction'da kaydeder. Domain event'lerinin outbox'a atomik yazılması
/// bu commit ile birlikte yapılır (docs/04 §10). Finance/Sales.IUnitOfWork deseniyle birebir.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
