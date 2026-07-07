using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using HalOS.Integration.Domain.ValueObjects;

namespace HalOS.Integration.Domain.Aggregates;

/// <summary>
/// e-Fatura (HAL / <c>Invoice</c>) — e-Belge &amp; Yasal Entegrasyon bağlamının kök aggregate'i
/// (docs/02 §1.2 / §3.5 <c>LegalDocument</c> alt tipi; docs/03 M8 / BK-4; docs/04 ADR-007/ADR-010).
/// Komisyoncunun ALICIYA kestiği faturadır; senaryo = <see cref="InvoiceScenario.Hal"/>, tür =
/// <see cref="InvoiceType.Commission"/> (aracılık komisyonu). Tenant'a bağlıdır (ITenantOwned → global
/// query filter, BK-8). Taraf/satış referansları ID ile (servisler arası FK yok — docs/05 §5).
///
/// Değişmezler (docs/02 §1.2 / §3.5, docs/03 BK-1/BK-4):
/// - e-Fatura ALICIYA kesilir (<see cref="BuyerPartyId"/>); e-MM (<see cref="ProducerReceipt"/>) ise
///   müstahsile — ikisi karıştırılmaz (docs/02 §7 anti-pattern).
/// - KOMİSYON türünde tutar = komisyon + komisyon KDV'si (docs/02 §1.2/§4). Bu tutarlar YENİDEN
///   HESAPLANMAZ; Sales'in taşıdığı <c>CommissionAmount</c> + <c>CommissionVatAmount</c> (SaleCompleted)
///   doğrudan kullanılır (docs/04 §10 event-taşımalı). Tutarlar negatif olamaz; komisyon pozitif olmalı.
/// - Bir satış (<see cref="SaleTransactionId"/>) tenant içinde en fazla BİR e-Fatura üretir (idempotency
///   anahtarı — docs/04 §5/§10 en-az-bir-kez teslimat). Consumer bu tekilliğe dayanır.
/// - Tamamlanmış/yasal belge SİLİNMEZ; iptal durum bayrağıyla (Cancelled — BK-9).
///
/// Tüm tutarlar <see cref="decimal"/> ve kuruşa yuvarlıdır (BK-2). Belge kesilince
/// <see cref="InvoiceIssued"/> event'i yayınlanır (outbox'a atomik — docs/04 §10). e-MM
/// (<see cref="ProducerReceipt"/>) aggregate deseniyle birebir.
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>, ITenantOwned
{
    private Invoice(
        Guid id,
        Guid tenantId,
        Guid saleTransactionId,
        Guid buyerPartyId,
        DateTime issueDate,
        InvoiceScenario scenario,
        InvoiceType type,
        decimal commissionAmount,
        decimal commissionVatAmount,
        decimal totalAmount)
        : base(id)
    {
        TenantId = tenantId;
        SaleTransactionId = saleTransactionId;
        BuyerPartyId = buyerPartyId;
        IssueDate = issueDate;
        Scenario = scenario;
        Type = type;
        CommissionAmount = commissionAmount;
        CommissionVatAmount = commissionVatAmount;
        TotalAmount = totalAmount;
        Status = InvoiceStatus.Draft;
    }

    /// <summary>ORM materialization only.</summary>
    private Invoice()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>e-Faturanın kaynağı satış (idempotency anahtarı — tenant içinde tekil; FK değil, docs/05 §5).</summary>
    public Guid SaleTransactionId { get; private set; }

    /// <summary>Alıcı referansı — fatura bu tarafa kesilir (Party ID; FK değil, docs/05 §5).</summary>
    public Guid BuyerPartyId { get; private set; }

    /// <summary>Belge düzenleme tarihi (satışın gerçekleştiği an).</summary>
    public DateTime IssueDate { get; private set; }

    /// <summary>e-Fatura senaryosu — halde HAL (docs/02 §1.2).</summary>
    public InvoiceScenario Scenario { get; private set; }

    /// <summary>e-Fatura türü — komisyoncu senaryosunda KOMİSYON (docs/02 §1.2).</summary>
    public InvoiceType Type { get; private set; }

    /// <summary>Komisyon tutarı (KDV hariç) — SaleCompleted'tan gelir (NUMERIC(18,2), BK-1).</summary>
    public decimal CommissionAmount { get; private set; }

    /// <summary>Komisyon üzerine hesaplanan KDV tutarı — SaleCompleted'tan gelir (NUMERIC(18,2), BK-1).</summary>
    public decimal CommissionVatAmount { get; private set; }

    /// <summary>Fatura toplam tutarı = komisyon + komisyon KDV'si (NUMERIC(18,2), BK-1).</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Fatura numarası — belge kesilince (Issued) atanır; öncesinde null.</summary>
    public string? InvoiceNumber { get; private set; }

    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Bir satıştan (SaleCompleted) yeni bir KOMİSYON e-Faturası taslağı (Draft) üretir (docs/03 M8 /
    /// BK-4). Tutarlar YENİDEN HESAPLANMAZ; Sales'in taşıdığı komisyon + komisyon KDV'si (event'ten)
    /// doğrudan kullanılır (docs/04 §10 event-taşımalı). Senaryo = HAL, tür = KOMİSYON (docs/02 §1.2).
    /// Toplam = komisyon + komisyon KDV'si, kuruşa yuvarlı.
    /// </summary>
    /// <remarks>
    /// e-Fatura HER başarılı satış için üretilir (BK-4 değişmez: her satış → en az bir e-Fatura); e-MM'in
    /// aksine KeepsRecords koşuluna bağlı DEĞİLDİR. Bu factory yalnız tutar/işaret değişmezlerini korur.
    /// </remarks>
    public static Result<Invoice> CreateCommission(
        Guid tenantId,
        Guid saleTransactionId,
        Guid buyerPartyId,
        DateTime issueDate,
        decimal commissionAmount,
        decimal commissionVatAmount)
    {
        if (saleTransactionId == Guid.Empty)
        {
            return Result.Failure<Invoice>(InvoiceErrors.SaleRequired);
        }

        if (buyerPartyId == Guid.Empty)
        {
            return Result.Failure<Invoice>(InvoiceErrors.BuyerRequired);
        }

        if (commissionAmount <= 0m)
        {
            return Result.Failure<Invoice>(InvoiceErrors.NonPositiveCommission);
        }

        if (commissionVatAmount < 0m)
        {
            return Result.Failure<Invoice>(InvoiceErrors.NegativeVat);
        }

        var commission = Money.RoundToKurus(commissionAmount);
        var vat = Money.RoundToKurus(commissionVatAmount);
        var total = Money.RoundToKurus(commission + vat);

        return new Invoice(
            Guid.NewGuid(),
            tenantId,
            saleTransactionId,
            buyerPartyId,
            issueDate,
            InvoiceScenario.Hal,
            InvoiceType.Commission,
            commission,
            vat,
            total);
    }

    /// <summary>
    /// Belgeyi GİB'e gönderilmiş/kesilmiş olarak işaretler ve fatura numarasını atar (docs/02 §3.5
    /// <c>DocumentIssued</c>/<c>InvoiceIssued</c>; docs/04 ADR-007 gönderim). <see cref="InvoiceIssued"/>
    /// event'i yayınlanır (outbox'a atomik — docs/04 §10). Yalnız Draft/Failed durumundan Issued'a
    /// geçilebilir; tekrar çağrı (idempotent) zararsızdır (zaten Issued ise event tekrar üretilmez).
    /// Fatura numarası zorunludur. e-MM.<c>MarkIssued</c> deseniyle birebir.
    /// </summary>
    public Result MarkIssued(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return Result.Failure(InvoiceErrors.InvoiceNumberRequired);
        }

        if (Status == InvoiceStatus.Cancelled)
        {
            return Result.Failure(InvoiceErrors.CancelledCannotIssue);
        }

        if (Status == InvoiceStatus.Issued)
        {
            // Zaten kesilmiş — idempotent tekrar; yeni event üretme (docs/04 §5).
            return Result.Success();
        }

        InvoiceNumber = invoiceNumber.Trim();
        Status = InvoiceStatus.Issued;

        RaiseDomainEvent(new InvoiceIssued(
            Id,
            TenantId,
            SaleTransactionId,
            BuyerPartyId,
            InvoiceNumber,
            TotalAmount,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Belgeyi gönderim başarısız olarak işaretler (docs/02 §3.5 <c>DocumentRejected</c>; docs/03 BK-4).
    /// Kullanıcı uyarılır; ADR-007 retry ile yeniden denenebilir. İptal edilmiş belge için geçersizdir.
    /// e-MM.<c>MarkFailed</c> deseniyle birebir.
    /// </summary>
    public Result MarkFailed()
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return Result.Failure(InvoiceErrors.CancelledCannotIssue);
        }

        Status = InvoiceStatus.Failed;
        return Result.Success();
    }

    /// <summary>e-Fatura domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
    public static class InvoiceErrors
    {
        public static readonly Error SaleRequired =
            new("Invoice.SaleRequired", "e-Fatura için satış referansı zorunludur.");

        public static readonly Error BuyerRequired =
            new("Invoice.BuyerRequired", "e-Fatura için alıcı referansı zorunludur.");

        public static readonly Error NonPositiveCommission =
            new("Invoice.NonPositiveCommission", "Komisyon tutarı sıfırdan büyük olmalıdır.");

        public static readonly Error NegativeVat =
            new("Invoice.NegativeVat", "Komisyon KDV tutarı negatif olamaz.");

        public static readonly Error InvoiceNumberRequired =
            new("Invoice.InvoiceNumberRequired", "Fatura numarası zorunludur.");

        public static readonly Error CancelledCannotIssue =
            new("Invoice.CancelledCannotIssue", "İptal edilmiş fatura kesilemez.");

        public static readonly Error NotFound =
            new("Invoice.NotFound", "e-Fatura bulunamadı.");
    }
}
