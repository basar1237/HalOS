namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// Değişiklikleri tek transaction'da kaydeder. Domain event'lerinin (SpoilageRecorded) outbox'a
/// atomik yazılması bu commit ile birlikte yapılır (docs/04 §10). Finance.IUnitOfWork deseniyle birebir.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
