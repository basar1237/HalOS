using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Domain.Enums;

namespace HalOS.Identity.Domain.Aggregates;

/// <summary>
/// Abonelik aggregate'i (docs/02 §1 <c>Subscription</c>, docs/06 S0.4 "abonelik iskeleti").
/// Bir tenant'ın plan/lisans durumunu tutar. Bu fazda iskelet düzeyinde.
/// </summary>
public sealed class Subscription : AggregateRoot<Guid>, ITenantOwned
{
    private Subscription(
        Guid id,
        Guid tenantId,
        SubscriptionPlan plan,
        SubscriptionStatus status,
        DateTime startsOnUtc,
        DateTime? endsOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        Plan = plan;
        Status = status;
        StartsOnUtc = startsOnUtc;
        EndsOnUtc = endsOnUtc;
    }

    private Subscription()
    {
    }

    public Guid TenantId { get; private set; }

    public SubscriptionPlan Plan { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTime StartsOnUtc { get; private set; }

    /// <summary>Bitiş; süresiz/otomatik yenilenen planlarda null.</summary>
    public DateTime? EndsOnUtc { get; private set; }

    /// <summary>Yeni tenant için varsayılan deneme aboneliği başlatır.</summary>
    public static Subscription StartTrial(Guid tenantId, int trialDays = 30)
    {
        var now = DateTime.UtcNow;
        return new Subscription(
            Guid.NewGuid(),
            tenantId,
            SubscriptionPlan.Trial,
            SubscriptionStatus.Active,
            now,
            now.AddDays(trialDays));
    }

    public void ChangePlan(SubscriptionPlan plan)
    {
        Plan = plan;
        Status = SubscriptionStatus.Active;
    }

    public void Suspend() => Status = SubscriptionStatus.Suspended;

    public void Cancel() => Status = SubscriptionStatus.Cancelled;
}
