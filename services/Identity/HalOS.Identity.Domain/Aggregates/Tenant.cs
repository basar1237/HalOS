using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Domain.Events;

namespace HalOS.Identity.Domain.Aggregates;

/// <summary>
/// İşletme (tenant) aggregate'i (docs/02 §1 <c>Tenant</c>). Multi-tenancy kökü;
/// diğer tüm iş entity'leri bu tenant'a <c>TenantId</c> ile bağlıdır (docs/04 ADR-008).
/// Tenant'ın kendisi kök olduğundan ITenantOwned değildir (kendi Id'si tenant kimliğidir).
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    private Tenant(Guid id, string name, DateTime createdOnUtc)
        : base(id)
    {
        Name = name;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    private Tenant()
    {
        Name = string.Empty;
    }

    /// <summary>İşletmenin görünen adı.</summary>
    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public static Result<Tenant> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tenant>(TenantErrors.NameRequired);
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            return Result.Failure<Tenant>(TenantErrors.NameTooLong);
        }

        var tenant = new Tenant(Guid.NewGuid(), trimmed, DateTime.UtcNow);
        tenant.RaiseDomainEvent(new TenantCreated(tenant.Id, tenant.Name, tenant.CreatedOnUtc));
        return tenant;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

public static class TenantErrors
{
    public static readonly Error NameRequired =
        new("Tenant.NameRequired", "İşletme adı zorunludur.");

    public static readonly Error NameTooLong =
        new("Tenant.NameTooLong", "İşletme adı çok uzun.");
}
