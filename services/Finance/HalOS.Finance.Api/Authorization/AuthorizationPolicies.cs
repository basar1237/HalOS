using Microsoft.AspNetCore.Authorization;

namespace HalOS.Finance.Api.Authorization;

/// <summary>
/// Finance servisi RBAC politikaları (docs/03 §3 yetki matrisi). Rol adları Identity'nin ürettiği
/// JWT rol claim'iyle eşleşir (Owner/Manager/Accountant/Cashier/Warehouse — docs/07 §3 İngilizce
/// kod adı). Erişim matrisi (docs/03 §3.1):
/// <list type="bullet">
///   <item>Cari görüntüle/ekstre/liste: Patron/Yönetici/Muhasebe (Kasiyer yalnız satış anında —
///     bu servis yüzeyinde tam ekstre için Muhasebe+Yönetici+Patron).</item>
///   <item>Ödeme yap (müstahsile): Patron/Yönetici/Muhasebe.</item>
///   <item>Tahsilat gir (alıcıdan): Patron/Yönetici/Muhasebe/Kasiyer.</item>
///   <item>Avans (ödeme benzeri mali işlem): Patron/Yönetici/Muhasebe.</item>
/// </list>
/// </summary>
public static class AuthorizationPolicies
{
    public const string CurrentAccountRead = "CurrentAccountRead";
    public const string PaymentWrite = "PaymentWrite";
    public const string CollectionWrite = "CollectionWrite";
    public const string AdvanceWrite = "AdvanceWrite";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Accountant = "Accountant";
    private const string Cashier = "Cashier";

    public static void AddFinancePolicies(this AuthorizationOptions options)
    {
        // Cari görüntüle/ekstre/liste: Patron/Yönetici/Muhasebe (docs/03 §3.1).
        options.AddPolicy(CurrentAccountRead, p => p.RequireRole(Owner, Manager, Accountant));

        // Müstahsile ödeme: Patron/Yönetici/Muhasebe (docs/03 §3.1).
        options.AddPolicy(PaymentWrite, p => p.RequireRole(Owner, Manager, Accountant));

        // Alıcıdan tahsilat: Patron/Yönetici/Muhasebe/Kasiyer (docs/03 §3.1).
        options.AddPolicy(CollectionWrite, p => p.RequireRole(Owner, Manager, Accountant, Cashier));

        // Avans (mali işlem): Patron/Yönetici/Muhasebe (docs/03 §3.1 ödeme yetkisiyle aynı çerçeve).
        options.AddPolicy(AdvanceWrite, p => p.RequireRole(Owner, Manager, Accountant));
    }
}
