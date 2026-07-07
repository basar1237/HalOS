namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// Transactional outbox record. Domain events are persisted here in the same DB
/// transaction as the state change, then dispatched asynchronously by a background
/// processor — guaranteeing no lost/duplicated event publications (docs/04 §10).
/// Carries <see cref="TenantId"/> to keep multi-tenant isolation intact (docs/07 §6).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Owning tenant (docs/07 §6). Nullable for system-level events.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>CLR type name of the event (used to deserialize <see cref="Content"/>).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Serialized event payload (JSON).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>When the event occurred / was enqueued (UTC).</summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>When the event was successfully dispatched (UTC); null while pending.</summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>Error captured on the last failed dispatch attempt; null on success.</summary>
    public string? Error { get; set; }
}
