using Microsoft.AspNetCore.Authorization;

namespace HalOS.ColdChain.Api.Authorization;

/// <summary>
/// ColdChain (Soğuk Zincir) servisi RBAC politikaları (docs/03 §3; docs/04 §6, docs/06 S3.1). Rol
/// adları Identity JWT rol claim'iyle eşleşir (Owner/Manager/Warehouse — docs/07 §3 İngilizce kod adı).
/// <list type="bullet">
///   <item>Depo görüntüle/liste/okuma listesi: Patron/Yönetici/Depo (okuma).</item>
///   <item>Depo tanımla/eşik güncelle: Patron/Yönetici (yapılandırma).</item>
///   <item>Sensör okuması gönder: Patron/Yönetici/Depo (saha/cihaz operatörü).</item>
/// </list>
/// </summary>
public static class AuthorizationPolicies
{
    public const string ColdChainRead = "ColdChainRead";
    public const string ColdChainWrite = "ColdChainWrite";
    public const string ReadingWrite = "ReadingWrite";

    private const string Owner = "Owner";
    private const string Manager = "Manager";
    private const string Warehouse = "Warehouse";

    public static void AddColdChainPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(ColdChainRead, p => p.RequireRole(Owner, Manager, Warehouse));
        options.AddPolicy(ColdChainWrite, p => p.RequireRole(Owner, Manager));
        options.AddPolicy(ReadingWrite, p => p.RequireRole(Owner, Manager, Warehouse));
    }
}
