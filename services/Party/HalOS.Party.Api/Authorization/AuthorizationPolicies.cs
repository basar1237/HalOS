using Microsoft.AspNetCore.Authorization;

namespace HalOS.Party.Api.Authorization;

/// <summary>
/// Party servisi RBAC politikaları (docs/03 §3). Rol adları Identity'nin ürettiği JWT rol
/// claim'iyle eşleşir (Owner/Manager/Accountant/Cashier/Warehouse — docs/07 §3 İngilizce kod adı).
///
/// Cari kart yazma (oluştur/güncelle/pasifleştir/rol ekle): Patron/Yönetici (Owner/Manager).
/// Cari kart okuma (görüntüle/listele): Muhasebe/Yönetici/Patron (Accountant/Manager/Owner).
/// </summary>
public static class AuthorizationPolicies
{
    public const string PartyWrite = "PartyWrite";
    public const string PartyRead = "PartyRead";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Accountant = "Accountant";

    public static void AddPartyPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(PartyWrite, p => p.RequireRole(Owner, Manager));
        options.AddPolicy(PartyRead, p => p.RequireRole(Owner, Manager, Accountant));
    }
}
