using HalOS.BuildingBlocks.Domain;

namespace HalOS.Integration.Domain.ReadModels;

/// <summary>
/// Müstahsile-özel vergi/kayıt profili okuma modeli (read-model) — Party servisinden gelen
/// <c>ProducerWithholdingProfileChanged</c> event'iyle senkronlanır (docs/02 §6; docs/04 §10,
/// ADR-008: servisler-arası veri event ile taşınır, FK yok — docs/05 §5). Integration servisi
/// e-MM üretimine karar verirken müstahsilin <see cref="KeepsRecords"/> bilgisini buradan okur:
/// e-MM YALNIZ kayıt TUTMAYAN müstahsil için üretilir (docs/05 §3.2, BK-4). Oran alanları
/// (stopaj/Bağ-Kur) ileride e-MM'i brüt+oranlardan yeniden kurmak/doğrulamak için tutulur; bu
/// slice'ta tutarlar SaleCompleted event'inden gelir (tek gerçeklik kaynağı Sales).
///
/// Tenant'a bağlıdır (ITenantOwned → global query filter, docs/07 §6 / BK-8). Bir müstahsil
/// (ProducerPartyId) tenant içinde en fazla bir profil satırına sahiptir. Oranlar
/// <see cref="decimal"/> (asla float/double — docs/07 §4 / BK-2); NUMERIC(7,4) ölçeği.
/// </summary>
public sealed class ProducerTaxProfile : Entity<Guid>, ITenantOwned
{
    private ProducerTaxProfile(
        Guid id,
        Guid tenantId,
        Guid producerPartyId,
        bool keepsRecords,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        DateTime updatedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        ProducerPartyId = producerPartyId;
        KeepsRecords = keepsRecords;
        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>ORM materialization only.</summary>
    private ProducerTaxProfile()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Müstahsil referansı (Party ID — FK değil, docs/05 §5). Tenant içinde tekil.</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>
    /// Müstahsil defter/kayıt tutuyor mu — e-MM gerekliliğini belirler (docs/05 §3.2, BK-4).
    /// <c>false</c> ise e-MM üretilir; <c>true</c> ise üretilmez.
    /// </summary>
    public bool KeepsRecords { get; private set; }

    /// <summary>Zirai stopaj oranı (docs/02 §1.3, NUMERIC(7,4)).</summary>
    public decimal AgriWithholdingRate { get; private set; }

    /// <summary>Çiftçi Bağ-Kur (SGK) primi oranı (docs/02 §1.3, NUMERIC(7,4)).</summary>
    public decimal FarmerSskRate { get; private set; }

    /// <summary>Son senkron zamanı (UTC) — Party event'inin işlendiği an (monoton guard için).</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Yeni bir okuma modeli satırı oluşturur (consumer ilk kez gördüğünde).</summary>
    public static ProducerTaxProfile Create(
        Guid tenantId,
        Guid producerPartyId,
        bool keepsRecords,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        DateTime updatedAtUtc) =>
        new(Guid.NewGuid(), tenantId, producerPartyId, keepsRecords, agriWithholdingRate, farmerSskRate, updatedAtUtc);

    /// <summary>
    /// Mevcut satırı en güncel değerlerle günceller (consumer upsert'in update kolu). Uygulama
    /// MONOTONdur: RabbitMQ sıra garantisi vermediğinden (docs/04 §10 en-az-bir-kez) sıra-dışı gelen
    /// ESKİ bir event, güncel bilgiyi bayat değerlerle geri almamalıdır. Bu yüzden yalnız gelen
    /// <paramref name="updatedAtUtc"/> mevcut <see cref="UpdatedAtUtc"/>'den büyük/eşitse uygulanır;
    /// daha eski event yok sayılır (bayat KeepsRecords → yanlış e-MM kararı, BK-4). Aynı zaman damgası
    /// (idempotent tekrar) zararsızca yeniden uygulanır. Sales'teki <c>ProducerRateProfile.Apply</c>
    /// deseniyle birebir.
    /// </summary>
    public void Apply(bool keepsRecords, decimal agriWithholdingRate, decimal farmerSskRate, DateTime updatedAtUtc)
    {
        if (updatedAtUtc < UpdatedAtUtc)
        {
            return;
        }

        KeepsRecords = keepsRecords;
        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
        UpdatedAtUtc = updatedAtUtc;
    }
}
