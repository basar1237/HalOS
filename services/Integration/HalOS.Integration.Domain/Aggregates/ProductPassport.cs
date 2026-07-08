using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;

namespace HalOS.Integration.Domain.Aggregates;

/// <summary>
/// Künye (<c>ProductPassport</c>) — e-Belge &amp; Yasal Entegrasyon bağlamının kök aggregate'i
/// (docs/02 §3.5 <c>ProductPassport</c>; docs/03 M8 / BK-4; docs/04 ADR-007/ADR-010). HKS'in ürettiği
/// ürün pasaportudur: <b>19-haneli kod</b> üretim yeri, tür, miktar, üretici ve sertifika bilgisini
/// kodlar; QR ile sorgulanır. Künye ÜRÜN/kalem bazlıdır → bir mal geliş kalemi
/// (<see cref="ConsignmentItemId"/>) için tek künye üretilir. Tenant'a bağlıdır (ITenantOwned → global
/// query filter, BK-8). Taraf/parti/ürün referansları ID ile (servisler arası FK yok — docs/05 §5).
///
/// Değişmezler (docs/02 §3.5 / §7, docs/03 BK-4):
/// - Künye HKS yasal kimliğidir; barkod/SKU ile KARIŞTIRILMAZ (docs/02 §7 anti-pattern).
/// - Miktar pozitif olmalı; parti/kalem/ürün referansları dolu olmalı.
/// - Bir mal geliş kalemi tenant içinde en fazla BİR künye üretir (idempotency anahtarı —
///   docs/04 §5/§10 en-az-bir-kez teslimat). Consumer bu tekilliğe dayanır.
///
/// Künye tescillenince (<see cref="MarkIssued"/>) <see cref="ProductPassportIssued"/> event'i
/// yayınlanır (outbox'a atomik — docs/04 §10). e-MM/e-Fatura (<see cref="ProducerReceipt"/>/
/// <see cref="Invoice"/>) aggregate deseniyle birebir.
/// </summary>
public sealed class ProductPassport : AggregateRoot<Guid>, ITenantOwned
{
    private ProductPassport(
        Guid id,
        Guid tenantId,
        Guid consignmentId,
        Guid consignmentItemId,
        Guid productId,
        Guid producerPartyId,
        decimal quantity,
        string unitCode,
        DateTime receivedAt)
        : base(id)
    {
        TenantId = tenantId;
        ConsignmentId = consignmentId;
        ConsignmentItemId = consignmentItemId;
        ProductId = productId;
        ProducerPartyId = producerPartyId;
        Quantity = quantity;
        UnitCode = unitCode;
        ReceivedAt = receivedAt;
        Status = ProductPassportStatus.Draft;
    }

    /// <summary>ORM materialization only.</summary>
    private ProductPassport()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Künyenin kaynağı mal geliş partisi (FK değil, docs/05 §5).</summary>
    public Guid ConsignmentId { get; private set; }

    /// <summary>Künyenin kaynağı parti kalemi (idempotency anahtarı — tenant içinde tekil; FK değil).</summary>
    public Guid ConsignmentItemId { get; private set; }

    /// <summary>Künyenin ait olduğu ürün referansı (Inventory ID'si — FK değil, docs/05 §5).</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Malı gönderen müstahsil/tüccar (Party ID; künye "üretici" bilgisi — FK değil, docs/05 §5).</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>Künyeye konu miktar (NUMERIC(18,3)).</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Birim kodu (Sales UnitOfMeasure.ToString() — event ile taşınır).</summary>
    public string UnitCode { get; private set; } = string.Empty;

    /// <summary>Malın kabul edildiği an (künye düzenleme referansı).</summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>HKS 19-haneli künye kodu — tescillenince (Issued) atanır; öncesinde null.</summary>
    public string? PassportCode { get; private set; }

    public ProductPassportStatus Status { get; private set; }

    /// <summary>
    /// Bir mal geliş kaleminden (ConsignmentReceived.Items) yeni bir künye taslağı (Draft) üretir
    /// (docs/03 M8 / BK-4). Değişmezler: miktar &gt; 0; parti/kalem/ürün referansları dolu.
    /// </summary>
    public static Result<ProductPassport> Create(
        Guid tenantId,
        Guid consignmentId,
        Guid consignmentItemId,
        Guid productId,
        Guid producerPartyId,
        decimal quantity,
        string unitCode,
        DateTime receivedAt)
    {
        if (consignmentId == Guid.Empty || consignmentItemId == Guid.Empty)
        {
            return Result.Failure<ProductPassport>(ProductPassportErrors.ConsignmentRequired);
        }

        if (productId == Guid.Empty)
        {
            return Result.Failure<ProductPassport>(ProductPassportErrors.ProductRequired);
        }

        if (quantity <= 0m)
        {
            return Result.Failure<ProductPassport>(ProductPassportErrors.NonPositiveQuantity);
        }

        return new ProductPassport(
            Guid.NewGuid(),
            tenantId,
            consignmentId,
            consignmentItemId,
            productId,
            producerPartyId,
            quantity,
            unitCode ?? string.Empty,
            receivedAt);
    }

    /// <summary>
    /// Künyeyi HKS'e tescillenmiş olarak işaretler ve 19-haneli künye kodunu atar (docs/02 §3.5;
    /// docs/04 ADR-007 gönderim). <see cref="ProductPassportIssued"/> event'i yayınlanır (outbox'a
    /// atomik — docs/04 §10). Yalnız Draft/Failed'dan Issued'a geçilebilir; tekrar çağrı (idempotent)
    /// zararsızdır (zaten Issued ise event tekrar üretilmez). Kod zorunludur.
    /// </summary>
    public Result MarkIssued(string passportCode)
    {
        if (string.IsNullOrWhiteSpace(passportCode))
        {
            return Result.Failure(ProductPassportErrors.PassportCodeRequired);
        }

        if (Status == ProductPassportStatus.Issued)
        {
            // Zaten tescilli — idempotent tekrar; yeni event üretme (docs/04 §5).
            return Result.Success();
        }

        PassportCode = passportCode.Trim();
        Status = ProductPassportStatus.Issued;

        RaiseDomainEvent(new ProductPassportIssued(
            Id,
            TenantId,
            ConsignmentId,
            ConsignmentItemId,
            ProductId,
            PassportCode,
            Quantity,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Künye üretimi/tescili başarısız olarak işaretlenir (docs/02 §3.5 <c>DocumentRejected</c>;
    /// docs/03 BK-4). Kullanıcı uyarılır; ADR-007 retry ile yeniden denenebilir.
    /// </summary>
    public Result MarkFailed()
    {
        Status = ProductPassportStatus.Failed;
        return Result.Success();
    }

    /// <summary>Künye domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
    public static class ProductPassportErrors
    {
        public static readonly Error ConsignmentRequired =
            new("ProductPassport.ConsignmentRequired", "Künye için mal geliş/kalem referansı zorunludur.");

        public static readonly Error ProductRequired =
            new("ProductPassport.ProductRequired", "Künye için ürün referansı zorunludur.");

        public static readonly Error NonPositiveQuantity =
            new("ProductPassport.NonPositiveQuantity", "Künye miktarı sıfırdan büyük olmalıdır.");

        public static readonly Error PassportCodeRequired =
            new("ProductPassport.PassportCodeRequired", "HKS künye kodu zorunludur.");

        public static readonly Error NotFound =
            new("ProductPassport.NotFound", "Künye (ürün pasaportu) bulunamadı.");
    }
}
