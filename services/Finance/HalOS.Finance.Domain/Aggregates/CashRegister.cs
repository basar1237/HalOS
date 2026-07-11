using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Domain.ValueObjects;

namespace HalOS.Finance.Domain.Aggregates;

/// <summary>Kasa türü: ticari kasa / rehin kasası (docs/11 §3.6).</summary>
public enum CashRegisterKind
{
    Commercial = 1, // Ticari kasa
    Pledge = 2,     // Rehin kasası
}

/// <summary>Kasa hareketi yönü: tahsil (giriş) / tediye (çıkış).</summary>
public enum CashDirection
{
    In = 1,  // Tahsil (kasaya giriş)
    Out = 2, // Tediye (kasadan çıkış)
}

/// <summary>Kasa hareketi (tahsil/tediye/virman kalemi). Kasa aggregate'inin parçası.</summary>
public sealed class CashMovement : Entity<Guid>, ITenantOwned
{
    private CashMovement(Guid id, Guid tenantId, Guid cashRegisterId, CashDirection direction, decimal amount, string? description, DateTime occurredAt)
        : base(id)
    {
        TenantId = tenantId;
        CashRegisterId = cashRegisterId;
        Direction = direction;
        Amount = amount;
        Description = description;
        OccurredAt = occurredAt;
    }

    private CashMovement() { }

    public Guid TenantId { get; private set; }
    public Guid CashRegisterId { get; private set; }
    public CashDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime OccurredAt { get; private set; }

    internal static CashMovement Create(Guid registerId, Guid tenantId, CashDirection direction, decimal amount, string? description, DateTime occurredAt) =>
        new(Guid.NewGuid(), tenantId, registerId, direction, Money.RoundToKurus(amount), string.IsNullOrWhiteSpace(description) ? null : description.Trim(), occurredAt);
}

/// <summary>
/// Kasa (docs/11 §3.6). Çoklu kasa (ticari/rehin), tahsil/tediye hareketleri; bakiye Σ hareket ile
/// türetilir (BK-2 kuruşa yuvarlı). Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8).
/// </summary>
public sealed class CashRegister : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CashMovement> _movements = new();

    private CashRegister(Guid id, Guid tenantId, string name, CashRegisterKind kind, DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Kind = kind;
        CreatedOnUtc = createdOnUtc;
    }

    private CashRegister() { }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CashRegisterKind Kind { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<CashMovement> Movements => _movements.AsReadOnly();

    /// <summary>Bakiye = Σ giriş − Σ çıkış (türetilmiş, kalıcı kolon değil).</summary>
    public decimal Balance =>
        Money.RoundToKurus(_movements.Sum(m => m.Direction == CashDirection.In ? m.Amount : -m.Amount));

    public static Result<CashRegister> Open(Guid tenantId, string? name, CashRegisterKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CashRegister>(CashErrors.NameRequired);
        }

        return new CashRegister(Guid.NewGuid(), tenantId, name.Trim(), kind, DateTime.UtcNow);
    }

    /// <summary>Kasaya hareket işler (tahsil/tediye). Yeni CashMovement döner (handler Added bildirir).</summary>
    public Result<CashMovement> Record(CashDirection direction, decimal amount, string? description, DateTime occurredAt)
    {
        if (amount <= 0m)
        {
            return Result.Failure<CashMovement>(CashErrors.InvalidAmount);
        }

        var movement = CashMovement.Create(Id, TenantId, direction, amount, description, occurredAt);
        _movements.Add(movement);
        return movement;
    }
}

public static class CashErrors
{
    public static readonly Error NameRequired = new("Cash.NameRequired", "Kasa adı zorunludur.");
    public static readonly Error InvalidAmount = new("Cash.InvalidAmount", "Tutar sıfırdan büyük olmalıdır.");
    public static readonly Error NotFound = new("Cash.NotFound", "Kasa bulunamadı.");
    public static readonly Error SameRegister = new("Cash.SameRegister", "Virman için farklı iki kasa seçilmelidir.");
}
