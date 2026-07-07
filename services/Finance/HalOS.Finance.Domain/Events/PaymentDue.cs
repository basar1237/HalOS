using HalOS.BuildingBlocks.Domain;

namespace HalOS.Finance.Domain.Events;

/// <summary>
/// Müstahsile hakediş kaydı bir cariye işlendiğinde, ödeme vade tarihiyle birlikte yayınlanır
/// (docs/02 §3.4 <c>PaymentDue</c>). Bildirim/AI proaktif hatırlatma için dinler (docs/02 §6).
/// Vade tarihi SaleCompleted'ın taşıdığı <c>SettlementDueDate</c>'tir (normal satış 15 iş günü —
/// BK-3). Event adı PascalCase geçmiş/durum bildiren (docs/07 §3).
/// </summary>
public sealed record PaymentDue(
    Guid CurrentAccountId,
    Guid TenantId,
    Guid ProducerPartyId,
    decimal NetAmount,
    DateTime DueDate,
    DateTime OccurredOnUtc) : IDomainEvent;
