using Microsoft.AspNetCore.Authorization;

namespace HalOS.Integration.Api.Authorization;

/// <summary>
/// Integration (e-Belge) servisi RBAC politikaları (docs/03 §3 yetki matrisi). Rol adları Identity'nin
/// ürettiği JWT rol claim'iyle eşleşir (Owner/Manager/Accountant — docs/07 §3 İngilizce kod adı).
/// e-Belge Merkezi (docs/03 §5): e-MM görüntüleme/liste ve yeniden gönderim (red yönetimi) mali/idari
/// işlemdir → Patron/Yönetici/Muhasebe.
/// </summary>
public static class AuthorizationPolicies
{
    public const string ProducerReceiptRead = "ProducerReceiptRead";
    public const string ProducerReceiptReissue = "ProducerReceiptReissue";

    // Identity.SystemRole kod adlarıyla birebir (servisler arası ortak sözleşme).
    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Accountant = "Accountant";

    public static void AddIntegrationPolicies(this AuthorizationOptions options)
    {
        // e-MM görüntüle/liste: Patron/Yönetici/Muhasebe (docs/03 §3/§5).
        options.AddPolicy(ProducerReceiptRead, p => p.RequireRole(Owner, Manager, Accountant));

        // e-MM yeniden gönder (red yönetimi): Patron/Yönetici/Muhasebe (docs/03 §5, BK-4).
        options.AddPolicy(ProducerReceiptReissue, p => p.RequireRole(Owner, Manager, Accountant));
    }
}
