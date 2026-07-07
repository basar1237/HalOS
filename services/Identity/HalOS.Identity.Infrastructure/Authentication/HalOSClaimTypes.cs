namespace HalOS.Identity.Infrastructure.Authentication;

/// <summary>
/// JWT'de kullanılan HalOS'a özgü claim adları. Token üretimi (Infrastructure) ile tenant
/// çözümleme middleware'i (Api) aynı sabitleri kullanır (docs/04 §7, docs/07 §6).
/// </summary>
public static class HalOSClaimTypes
{
    public const string TenantId = "tenant_id";

    public const string Role = "role";
}
