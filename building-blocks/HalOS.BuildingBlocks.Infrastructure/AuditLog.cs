namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// Değiştirilemez (append-only) denetim kaydı: bir komut çalıştığında "kim / ne / ne zaman"ı
/// kalıcılaştırır (docs/05 §3.11, docs/03 §6 "tüm mali işlemler audit log'lu", docs/04 §201
/// "değiştirilemez"). <see cref="OutboxMessage"/> ile aynı desende, tüm servislerin DB'sinde
/// paylaşılan <c>audit_log</c> tablosuna eşlenir. Bir kez yazıldıktan sonra GÜNCELLENMEZ / SİLİNMEZ.
///
/// <para><b>Faz 1 sınırları (bilinçli, plan sahibi onaylı):</b>
/// (m4) Append-only ŞU AN yalnızca uygulama katmanında güvence altındadır (sink yalnız
/// <c>Add</c> yapar; UPDATE/DELETE üretmez). DB düzeyinde REVOKE UPDATE/DELETE ve/veya
/// değiştirmeyi/silmeyi engelleyen trigger <b>Faz 2</b>'ye bırakılmıştır (docs/04 §201 tam
/// zorlaması için).
/// (m5) <see cref="AuditLog"/> bilinçli olarak <c>ITenantOwned</c> DEĞİLDİR: <see cref="TenantId"/>
/// nullable'dır (sistem/anonim bağlam) ve global query filter uygulanMAZ. Bu yüzden Faz 2'de
/// eklenecek denetim OKUMA API'leri tenant izolasyonunu (BK-8) <b>elle</b>
/// (<c>WHERE tenant_id = @current</c>) uygulamak ZORUNDADIR; aksi halde çapraz-tenant sızıntı olur.</para>
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kaydın ait olduğu tenant (docs/07 §6 / BK-8). Sistem/anonim bağlamda null olabilir.
    /// Global query filter UYGULANMAZ (bkz. sınıf doc m5); okuma tarafında elle filtrelenmelidir.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Komutu yürüten kullanıcı (kim). Anonim/sistem bağlamında null.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Yürütülen eylem — komut CLR tip adı (örn. <c>CreateSaleCommand</c>).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>İlgili entity/aggregate tipinin adı (varsa).</summary>
    public string? EntityType { get; set; }

    /// <summary>İlgili entity kimliği (varsa; serbest metin — Guid/kompozit olabilir).</summary>
    public string? EntityId { get; set; }

    /// <summary>Değişiklik öncesi durum (JSON; varsa). Faz 1'de opsiyonel.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>Değişiklik sonrası durum (JSON; varsa). Faz 1'de opsiyonel.</summary>
    public string? AfterJson { get; set; }

    /// <summary>Kaydın oluşturulduğu an (UTC — "ne zaman").</summary>
    public DateTime CreatedOnUtc { get; set; }
}
