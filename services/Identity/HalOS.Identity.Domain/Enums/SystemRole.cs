namespace HalOS.Identity.Domain.Enums;

/// <summary>
/// Sistemin öntanımlı rolleri (docs/03 §3 yetki matrisi). RBAC politikaları bu
/// rollere dayanır. Kod adı İngilizce (docs/07 §3); kullanıcıya görünen ad Türkçe.
/// </summary>
public enum SystemRole
{
    /// <summary>Patron — tam yetki.</summary>
    Owner = 1,

    /// <summary>Yönetici — operasyonel yönetim.</summary>
    Manager = 2,

    /// <summary>Muhasebe — mali kayıt/rapor.</summary>
    Accountant = 3,

    /// <summary>Kasiyer — satış/tahsilat.</summary>
    Cashier = 4,

    /// <summary>Depo — stok/mal geliş.</summary>
    Warehouse = 5
}
