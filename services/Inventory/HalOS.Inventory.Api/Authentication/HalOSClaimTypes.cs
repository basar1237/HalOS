namespace HalOS.Inventory.Api.Authentication;

/// <summary>
/// JWT'de kullanılan HalOS'a özgü claim adları. Identity servisinin ürettiği token'larla aynı
/// sabitler kullanılır (docs/04 §7, docs/07 §6). Servisler arası bağımlılık kod paylaşımıyla değil,
/// ortak sözleşme (claim adı) ile sağlanır.
/// </summary>
public static class HalOSClaimTypes
{
    public const string TenantId = "tenant_id";

    public const string Role = "role";
}
