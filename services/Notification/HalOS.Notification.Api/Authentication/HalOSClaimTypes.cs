namespace HalOS.Notification.Api.Authentication;

/// <summary>
/// JWT'de kullanılan HalOS'a özgü claim adları. Identity servisinin ürettiği token'larla aynı
/// sabitler kullanılır (docs/04 §7, docs/07 §6). SignalR hub bağlantısı, kullanıcının katılacağı
/// tenant grubunu <see cref="TenantId"/> claim'inden belirler — istemci grubu SEÇEMEZ (BK-8).
/// </summary>
public static class HalOSClaimTypes
{
    public const string TenantId = "tenant_id";

    public const string Role = "role";
}
