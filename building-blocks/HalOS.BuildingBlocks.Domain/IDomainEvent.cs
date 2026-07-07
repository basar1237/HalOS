namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// Marker for a domain event. Domain events are raised by aggregates and dispatched by the
/// Infrastructure layer via the transactional outbox (docs/04 §10) — the Domain layer stays
/// free of any external package (docs/07 §2), so this interface intentionally does NOT extend
/// MediatR's <c>INotification</c>. Events are named in past tense (e.g. SaleCompleted), per
/// docs/07 §3.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Instant the event occurred (UTC).</summary>
    DateTime OccurredOnUtc { get; }
}
