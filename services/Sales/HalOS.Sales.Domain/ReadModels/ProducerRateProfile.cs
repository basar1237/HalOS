using HalOS.BuildingBlocks.Domain;

namespace HalOS.Sales.Domain.ReadModels;

/// <summary>
/// Müstahsile-özel oran okuma modeli (read-model) — Party servisinden gelen
/// <c>ProducerWithholdingProfileChanged</c> event'iyle senkronlanır (docs/02 §6; docs/04 §10,
/// ADR-008: servisler-arası veri event ile taşınır, FK yok — docs/05 §5). <c>IRateProvider</c>
/// satış anında oranları önce buradan çözer, eksik alanları tenant config'e düşürür; böylece
/// hakediş müstahsilin gerçek oranlarıyla hesaplanır.
///
/// Tenant'a bağlıdır (ITenantOwned → global query filter, docs/07 §6 / BK-8). Bir müstahsil
/// (ProducerPartyId) tenant içinde en fazla bir profil satırına sahiptir. Oranlar
/// <see cref="decimal"/> (asla float/double — docs/07 §4 / BK-2); NUMERIC(7,4) ölçeği. Yalnızca
/// Party'de GERÇEKTEN var olan oranları tutar (zirai stopaj + çiftçi Bağ-Kur); komisyon Party'de
/// tutulmadığından burada da yer almaz (tenant config'ten çözülür — docs/02 §4).
/// </summary>
public sealed class ProducerRateProfile : Entity<Guid>, ITenantOwned
{
    private ProducerRateProfile(
        Guid id,
        Guid tenantId,
        Guid producerPartyId,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        DateTime updatedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        ProducerPartyId = producerPartyId;
        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>ORM materialization only.</summary>
    private ProducerRateProfile()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Müstahsil referansı (Party ID — FK değil, docs/05 §5). Tenant içinde tekil.</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>Zirai stopaj oranı (docs/02 §1.3 <c>AgriculturalWithholding</c>, NUMERIC(7,4)).</summary>
    public decimal AgriWithholdingRate { get; private set; }

    /// <summary>Çiftçi Bağ-Kur (SGK) primi oranı (docs/02 §1.3 <c>FarmerSocialSecurity</c>, NUMERIC(7,4)).</summary>
    public decimal FarmerSskRate { get; private set; }

    /// <summary>Son senkron zamanı (UTC) — Party event'inin işlendiği an.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Yeni bir okuma modeli satırı oluşturur (consumer ilk kez gördüğünde).</summary>
    public static ProducerRateProfile Create(
        Guid tenantId,
        Guid producerPartyId,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        DateTime updatedAtUtc) =>
        new(Guid.NewGuid(), tenantId, producerPartyId, agriWithholdingRate, farmerSskRate, updatedAtUtc);

    /// <summary>
    /// Mevcut satırı en güncel oranlarla günceller (consumer upsert'in update kolu). Uygulama
    /// MONOTONdur: RabbitMQ sıra garantisi vermediğinden (docs/04 §10 en-az-bir-kez) sıra-dışı
    /// gelen ESKİ bir event, güncel oranları bayat değerlerle geri almamalıdır. Bu yüzden yalnız
    /// gelen <paramref name="updatedAtUtc"/> mevcut <see cref="UpdatedAtUtc"/>'den büyük/eşitse
    /// uygulanır; daha eski event yok sayılır (bayat oran → yanlış zirai stopaj/Bağ-Kur → yanlış
    /// net hakediş, BK-1). Aynı zaman damgası (idempotent tekrar) zararsızca yeniden uygulanır.
    /// </summary>
    public void Apply(decimal agriWithholdingRate, decimal farmerSskRate, DateTime updatedAtUtc)
    {
        if (updatedAtUtc < UpdatedAtUtc)
        {
            return;
        }

        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
        UpdatedAtUtc = updatedAtUtc;
    }
}
