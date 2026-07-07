namespace HalOS.Party.Domain.Enums;

/// <summary>
/// Bir <c>Party</c>'nin taşıyabileceği roller (docs/02 §1.1, §3.1). Bir taraf birden çok rol
/// taşıyabilir (örn. hem <see cref="Producer"/> hem <see cref="Consignor"/>). Kod adı
/// İngilizce (docs/07 §3); kullanıcıya görünen ad Türkçe.
/// </summary>
public enum PartyRoleType
{
    /// <summary>Müstahsil — sebze-meyve üreticisi (çiftçi). Stopaj profili zorunludur.</summary>
    Producer = 1,

    /// <summary>Alıcı (perakendeci) — malı toptan alıp tüketiciye satan.</summary>
    Buyer = 2,

    /// <summary>Tüccar — kendi adına ve hesabına mal alıp satan.</summary>
    Merchant = 3,

    /// <summary>Taşıyıcı (sevkiyatçı) — malı başka haldeki komisyoncuya gönderen ara aktör.</summary>
    Consignor = 4
}
