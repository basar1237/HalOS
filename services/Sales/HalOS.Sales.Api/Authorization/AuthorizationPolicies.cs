using Microsoft.AspNetCore.Authorization;

namespace HalOS.Sales.Api.Authorization;

/// <summary>
/// Sales servisi RBAC politikaları (docs/03 §3 yetki matrisi). Rol adları Identity'nin ürettiği
/// JWT rol claim'iyle eşleşir (Owner/Manager/Accountant/Cashier/Warehouse — docs/07 §3 İngilizce
/// kod adı).
///
/// - Satış kaydı oluştur / satır ekle / tamamla: Patron/Yönetici/Kasiyer (Owner/Manager/Cashier).
/// - Mal geliş kabul: Patron/Yönetici/Kasiyer/Depo (Owner/Manager/Cashier/Warehouse).
/// - Satış iptal: kısıtlı → Patron/Yönetici (Owner/Manager). (Kasiyerin kendi/onaysız satışı
///   iptali daha ince kural gerektirir; MVP'de iptal Patron/Yönetici ile kısıtlandı — docs/03 §3.)
/// - Kesinti/oran değiştir: yalnızca Patron/Yönetici (Owner/Manager). (Oran değişimi bu servis
///   yüzeyinde ayrı uç değil; config/Party profili üzerinden — ilgili politika ileride kullanılır.)
/// - Okuma (görüntüle/listele): Patron/Yönetici/Muhasebe/Kasiyer.
/// </summary>
public static class AuthorizationPolicies
{
    public const string SaleWrite = "SaleWrite";
    public const string SaleCancel = "SaleCancel";
    public const string RateChange = "RateChange";
    public const string ConsignmentWrite = "ConsignmentWrite";
    public const string SaleRead = "SaleRead";

    /// <summary>Satış raporları okuma (docs/03 M10): Patron/Yönetici/Muhasebe.</summary>
    public const string SalesReportRead = "SalesReportRead";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Accountant = "Accountant";
    private const string Cashier = "Cashier";
    private const string Warehouse = "Warehouse";

    public static void AddSalesPolicies(this AuthorizationOptions options)
    {
        // Satış oluştur/satır/tamamla: Patron/Yönetici/Kasiyer (docs/03 §3).
        options.AddPolicy(SaleWrite, p => p.RequireRole(Owner, Manager, Cashier));

        // Satış iptal kısıtlı: Patron/Yönetici (docs/03 §3).
        options.AddPolicy(SaleCancel, p => p.RequireRole(Owner, Manager));

        // Oran değiştir: yalnızca Patron/Yönetici (docs/03 §3).
        options.AddPolicy(RateChange, p => p.RequireRole(Owner, Manager));

        // Mal geliş kabul: Patron/Yönetici/Kasiyer/Depo (docs/03 §3).
        options.AddPolicy(ConsignmentWrite, p => p.RequireRole(Owner, Manager, Cashier, Warehouse));

        // Okuma: Patron/Yönetici/Muhasebe/Kasiyer.
        options.AddPolicy(SaleRead, p => p.RequireRole(Owner, Manager, Accountant, Cashier));

        // Satış raporları okuma (docs/03 M10): Patron/Yönetici/Muhasebe (finansal özet — Kasiyer hariç).
        options.AddPolicy(SalesReportRead, p => p.RequireRole(Owner, Manager, Accountant));
    }
}
