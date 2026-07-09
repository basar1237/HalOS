using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Domain.Aggregates;

/// <summary>
/// Ürün kataloğu kaydı (docs/03 M2 "Ürün & Birim"; docs/05 §3.3 <c>product</c>). Satış satırı ve mal
/// geliş kalemi ürünü ID ile referanslar (servisler-arası FK yok — docs/05 §5); bu aggregate o
/// ürünlerin adı/kategorisi/varsayılan birimi için TEK doğruluk kaynağıdır. Tenant'a bağlıdır
/// (ITenantOwned → global query filter, BK-8). Warehouse aggregate deseniyle birebir (statik fabrika
/// + Result, kapsüllenmiş setter). Pasifleştirme soft-delete'tir (IsActive=false; kayıt SİLİNMEZ).
/// </summary>
public sealed class Product : AggregateRoot<Guid>, ITenantOwned
{
    private Product(
        Guid id,
        Guid tenantId,
        string name,
        string? category,
        UnitOfMeasure defaultUnit)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Category = category;
        DefaultUnit = defaultUnit;
        IsActive = true;
    }

    /// <summary>ORM materialization only.</summary>
    private Product()
    {
        Name = string.Empty;
    }

    public Guid TenantId { get; private set; }

    /// <summary>Ürünün görünen adı (ör. "Domates").</summary>
    public string Name { get; private set; }

    /// <summary>Ürün kategorisi (ör. "Sebze"); opsiyonel.</summary>
    public string? Category { get; private set; }

    /// <summary>Varsayılan ölçü birimi (satış/mal-geliş satırında ön-seçili gelir).</summary>
    public UnitOfMeasure DefaultUnit { get; private set; }

    /// <summary>Aktif mi? Pasif ürün seçicide gösterilmez ama kayıtları korunur (soft-delete).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Yeni ürün oluşturur. Ad zorunludur; kategori opsiyonel.</summary>
    public static Result<Product> Create(
        Guid tenantId,
        string name,
        string? category,
        UnitOfMeasure defaultUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(ProductErrors.NameRequired);
        }

        return new Product(
            Guid.NewGuid(),
            tenantId,
            name.Trim(),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            defaultUnit);
    }

    /// <summary>Ad/kategori/varsayılan birim günceller. Ad boş olamaz.</summary>
    public Result Update(string name, string? category, UnitOfMeasure defaultUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ProductErrors.NameRequired);
        }

        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        DefaultUnit = defaultUnit;
        return Result.Success();
    }

    /// <summary>Ürünü pasifleştirir (soft-delete). Zaten pasifse etkisizdir.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Ürün domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
public static class ProductErrors
{
    public static readonly Error NameRequired =
        new("Product.NameRequired", "Ürün adı zorunludur.");

    public static readonly Error NotFound =
        new("Product.NotFound", "Ürün bulunamadı.");
}
