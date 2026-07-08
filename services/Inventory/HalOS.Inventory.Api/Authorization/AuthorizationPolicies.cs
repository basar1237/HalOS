using Microsoft.AspNetCore.Authorization;

namespace HalOS.Inventory.Api.Authorization;

/// <summary>
/// Inventory servisi RBAC politikaları (docs/03 §3 yetki matrisi; M9/BK-7). Rol adları Identity'nin
/// ürettiği JWT rol claim'iyle eşleşir (Owner/Manager/Accountant/Cashier/Warehouse — docs/07 §3
/// İngilizce kod adı). Erişim matrisi (docs/03 §3, Stok/fire: Patron/Yönetici/Depo):
/// <list type="bullet">
///   <item>Stok görüntüle/liste/hareket: Patron/Yönetici/Depo.</item>
///   <item>Fire kaydet: Patron/Yönetici/Depo (BK-7).</item>
/// </list>
/// </summary>
public static class AuthorizationPolicies
{
    public const string StockRead = "StockRead";
    public const string SpoilageWrite = "SpoilageWrite";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Warehouse = "Warehouse";

    public static void AddInventoryPolicies(this AuthorizationOptions options)
    {
        // Stok görüntüle/liste/hareket: Patron/Yönetici/Depo (docs/03 §3).
        options.AddPolicy(StockRead, p => p.RequireRole(Owner, Manager, Warehouse));

        // Fire kaydet: Patron/Yönetici/Depo (docs/03 §3; BK-7).
        options.AddPolicy(SpoilageWrite, p => p.RequireRole(Owner, Manager, Warehouse));
    }
}
