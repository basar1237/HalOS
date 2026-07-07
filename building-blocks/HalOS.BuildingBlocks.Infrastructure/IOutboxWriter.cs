using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// Enqueues domain events into the transactional outbox. Handlers/aggregates never publish
/// events directly (no in-handler HTTP calls — docs/07 §5); they write to the outbox and the
/// events are dispatched after the unit of work commits (docs/04 §10).
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Stages a single domain event for publication. The record is expected to be persisted
    /// within the same transaction as the state change (i.e. on the next SaveChanges).
    /// </summary>
    void Write(IDomainEvent domainEvent);

    /// <summary>Stages multiple domain events for publication.</summary>
    void Write(IEnumerable<IDomainEvent> domainEvents);
}
