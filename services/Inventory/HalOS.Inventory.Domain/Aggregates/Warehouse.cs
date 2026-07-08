using HalOS.BuildingBlocks.Domain;

namespace HalOS.Inventory.Domain.Aggregates;

/// <summary>
/// Depo (Warehouse) — gelişmiş stok bağlamının depo lokasyonu aggregate'i (docs/06 S2.1 depo
/// lokasyonu). Stok kalemleri (<see cref="StockItem"/>) artık (tenant, depo, ürün) bazlıdır; her
/// tenant'ın en az bir VARSAYILAN deposu olur (<see cref="IsDefault"/>). Tenant'a bağlıdır
/// (ITenantOwned → global query filter, BK-8). Kod (<see cref="Code"/>) tenant içinde tekildir
/// (UNIQUE(tenant_id, code)). Finance <c>CurrentAccount</c>/Inventory <c>StockItem</c> aggregate
/// deseniyle birebir (statik fabrika + Result, kapsüllenmiş setter).
/// </summary>
public sealed class Warehouse : AggregateRoot<Guid>, ITenantOwned
{
    private Warehouse(Guid id, Guid tenantId, string name, string code, bool isDefault)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        IsDefault = isDefault;
    }

    /// <summary>ORM materialization only.</summary>
    private Warehouse()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public Guid TenantId { get; private set; }

    /// <summary>Deponun görünen adı (ör. "Merkez Depo").</summary>
    public string Name { get; private set; }

    /// <summary>Deponun kısa kodu; tenant içinde tekil (UNIQUE(tenant_id, code)).</summary>
    public string Code { get; private set; }

    /// <summary>
    /// Bu depo tenant'ın varsayılan deposu mu? Olay-güdümlü stok girişi/çıkışı (Consignment/Sale)
    /// warehouse taşımadığından varsayılan depoya yazılır (docs/06 S2.1 notu). Tenant başına EN FAZLA
    /// bir varsayılan depo olur (tekillik değişmezi); yeni bir varsayılan atandığında eski varsayılan
    /// <see cref="Demote"/> ile düşürülür. Bu değişmez uygulamada handler'da, veritabanında ise
    /// kısmi tekil indeks (partial unique index, WHERE is_default) ile çift savunmalı korunur.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>Yeni bir depo oluşturur. Ad ve kod zorunludur.</summary>
    /// <param name="tenantId">Deponun bağlı olduğu işletme (tenant).</param>
    /// <param name="name">Deponun görünen adı.</param>
    /// <param name="code">Tenant içinde tekil kısa kod.</param>
    /// <param name="isDefault">Varsayılan depo olarak işaretlenip işaretlenmeyeceği.</param>
    public static Result<Warehouse> Create(Guid tenantId, string name, string code, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Warehouse>(WarehouseErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Warehouse>(WarehouseErrors.CodeRequired);
        }

        return new Warehouse(Guid.NewGuid(), tenantId, name.Trim(), code.Trim(), isDefault);
    }

    /// <summary>
    /// Bu depoyu varsayılan konumundan düşürür (<see cref="IsDefault"/> = false). Yeni bir depo
    /// varsayılan yapıldığında tenant başına tek varsayılan depo değişmezini korumak için mevcut
    /// varsayılan(lar) bu metotla düşürülür (docs/06 S2.1). Zaten varsayılan değilse etkisizdir.
    /// </summary>
    public void Demote() => IsDefault = false;
}

/// <summary>Depo domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
public static class WarehouseErrors
{
    public static readonly Error NameRequired =
        new("Warehouse.NameRequired", "Depo adı zorunludur.");

    public static readonly Error CodeRequired =
        new("Warehouse.CodeRequired", "Depo kodu zorunludur.");

    public static readonly Error CodeAlreadyExists =
        new("Warehouse.CodeAlreadyExists", "Bu depo kodu işletme içinde zaten kullanılıyor.");

    public static readonly Error NotFound =
        new("Warehouse.NotFound", "Depo bulunamadı.");
}
