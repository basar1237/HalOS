using Microsoft.AspNetCore.Authorization;

namespace HalOS.Search.Api.Authorization;

/// <summary>
/// Search servisi RBAC politikaları (docs/03 §3 yetki matrisi). Rol adları Identity'nin ürettiği JWT
/// rol claim'iyle eşleşir (docs/07 §3 İngilizce kod adı). Arama okuma odaklıdır:
/// Patron/Yönetici/Muhasebe/Kasiyer görüntüleyebilir (docs/06 S2.3 — günlük operasyon aramaları).
/// Depo (Warehouse) satış/cari araması yapmaz → dahil değil.
/// </summary>
public static class AuthorizationPolicies
{
    public const string SearchRead = "SearchRead";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Accountant = "Accountant";
    private const string Cashier = "Cashier";

    public static void AddSearchPolicies(this AuthorizationOptions options)
    {
        // Arama okuma: Patron/Yönetici/Muhasebe/Kasiyer (docs/03 §3, docs/06 S2.3).
        options.AddPolicy(SearchRead, p => p.RequireRole(Owner, Manager, Accountant, Cashier));
    }
}
