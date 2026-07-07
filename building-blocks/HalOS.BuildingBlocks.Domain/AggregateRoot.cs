namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots. Maintains an in-memory queue of domain events
/// raised during a use case; these are collected and published by the Infrastructure
/// layer (via the outbox) after the unit of work commits — see docs/04 §10.
/// </summary>
/// <typeparam name="TId">Identity type of the aggregate.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>Events raised by this aggregate that have not yet been dispatched.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Clears the queue; called after events have been collected for dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
