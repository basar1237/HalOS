using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using HalOS.Integration.Domain.ValueObjects;

namespace HalOS.Integration.Domain.Aggregates;

/// <summary>
/// e-Müstahsil Makbuzu (e-MM / <c>ProducerReceipt</c>) — e-Belge &amp; Yasal Entegrasyon bağlamının
/// kök aggregate'i (docs/02 §3.5 <c>LegalDocument</c> alt tipi; docs/03 M7 / BK-4; docs/04 ADR-007).
/// Kayıt TUTMAYAN müstahsilden yapılan alımda düzenlenen, stopaj/kesinti içeren yasal belgedir
/// (docs/02 §1.3). Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8). Taraf/satış
/// referansları ID ile (servisler arası FK yok — docs/05 §5).
///
/// Değişmezler (docs/02 §1.3 / §3.5, docs/03 BK-1/BK-4):
/// - e-MM YALNIZ stopaj + çiftçi Bağ-Kur kesintisini içerir; komisyon/hal rüsumu/komisyon KDV'si
///   e-MM'e GİRMEZ (bunlar komisyoncu-alıcı ilişkisine aittir — docs/02 §1.2, BK-1).
/// - <c>NetPayable = GrossAmount − (stopaj + Bağ-Kur)</c> (yalnız bu belgedeki kesintiler). Net
///   NEGATİF olamaz; kesintiler negatif olamaz; brüt pozitif olmalıdır.
/// - Bir satış (<see cref="SaleTransactionId"/>) tenant içinde en fazla BİR e-MM üretir (idempotency
///   anahtarı — docs/04 §5/§10 en-az-bir-kez teslimat). Consumer bu tekilliğe dayanır.
/// - Tamamlanmış/yasal belge SİLİNMEZ; iptal durum bayrağıyla (Cancelled — BK-9).
///
/// Tüm tutarlar <see cref="decimal"/> ve kuruşa yuvarlıdır (BK-2). Belge kesilince
/// <see cref="ProducerReceiptIssued"/> event'i yayınlanır (outbox'a atomik — docs/04 §10).
/// </summary>
public sealed class ProducerReceipt : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ReceiptDeduction> _deductions = new();

    private ProducerReceipt(
        Guid id,
        Guid tenantId,
        Guid saleTransactionId,
        Guid producerPartyId,
        Guid buyerPartyId,
        DateTime issueDate,
        decimal grossAmount,
        decimal agriWithholdingAmount,
        decimal farmerSskAmount,
        decimal netPayable)
        : base(id)
    {
        TenantId = tenantId;
        SaleTransactionId = saleTransactionId;
        ProducerPartyId = producerPartyId;
        BuyerPartyId = buyerPartyId;
        IssueDate = issueDate;
        GrossAmount = grossAmount;
        AgriWithholdingAmount = agriWithholdingAmount;
        FarmerSskAmount = farmerSskAmount;
        NetPayable = netPayable;
        Status = ProducerReceiptStatus.Draft;

        _deductions.Add(ReceiptDeduction.Create(Id, tenantId, ReceiptDeductionType.AgriWithholding, agriWithholdingAmount));
        _deductions.Add(ReceiptDeduction.Create(Id, tenantId, ReceiptDeductionType.FarmerSsk, farmerSskAmount));
    }

    /// <summary>ORM materialization only.</summary>
    private ProducerReceipt()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>e-MM'in kaynağı satış (idempotency anahtarı — tenant içinde tekil; FK değil, docs/05 §5).</summary>
    public Guid SaleTransactionId { get; private set; }

    /// <summary>Müstahsil referansı — belge bu tarafa düzenlenir (Party ID; FK değil, docs/05 §5).</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>Alıcı referansı (Party ID; FK değil, docs/05 §5). Belgede bilgi amaçlı taşınır.</summary>
    public Guid BuyerPartyId { get; private set; }

    /// <summary>Belge düzenleme tarihi (satışın gerçekleştiği an).</summary>
    public DateTime IssueDate { get; private set; }

    /// <summary>Brüt satış bedeli — kesinti öncesi (NUMERIC(18,2), BK-2).</summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>Zirai stopaj kesinti tutarı — e-MM'e girer (NUMERIC(18,2)).</summary>
    public decimal AgriWithholdingAmount { get; private set; }

    /// <summary>Çiftçi Bağ-Kur (SGK) primi kesinti tutarı — e-MM'e girer (NUMERIC(18,2)).</summary>
    public decimal FarmerSskAmount { get; private set; }

    /// <summary>Müstahsile ödenecek net = brüt − (stopaj + Bağ-Kur). Negatif olamaz (NUMERIC(18,2), BK-1).</summary>
    public decimal NetPayable { get; private set; }

    /// <summary>Makbuz numarası — belge kesilince (Issued) atanır; öncesinde null.</summary>
    public string? ReceiptNumber { get; private set; }

    public ProducerReceiptStatus Status { get; private set; }

    /// <summary>e-MM kesinti kalemleri (yalnız stopaj + Bağ-Kur; AYRI satırlar — docs/02 §7).</summary>
    public IReadOnlyCollection<ReceiptDeduction> Deductions => _deductions.AsReadOnly();

    /// <summary>
    /// Bir satıştan (SaleCompleted) yeni bir e-MM taslağı (Draft) üretir (docs/03 M7 / BK-4).
    /// Kesinti tutarları YENİDEN HESAPLANMAZ; Sales'in taşıdığı stopaj + Bağ-Kur tutarları
    /// (event'ten) doğrudan kullanılır (docs/04 §10 event-taşımalı). e-MM'e komisyon/rüsum/KDV
    /// GİRMEZ (BK-1/BK-4). Net = brüt − (stopaj + Bağ-Kur), kuruşa yuvarlı; negatif olamaz.
    /// </summary>
    /// <remarks>
    /// KeepsRecords kontrolü ÇAĞIRAN katmandadır (SaleCompletedConsumer): e-MM yalnız kayıt TUTMAYAN
    /// müstahsil için üretilir; profil bilinmiyorsa üretilmez (temkinli — yasal belge). Bu factory
    /// yalnız tutar/işaret değişmezlerini korur.
    /// </remarks>
    public static Result<ProducerReceipt> Create(
        Guid tenantId,
        Guid saleTransactionId,
        Guid producerPartyId,
        Guid buyerPartyId,
        DateTime issueDate,
        decimal grossAmount,
        decimal agriWithholdingAmount,
        decimal farmerSskAmount)
    {
        if (saleTransactionId == Guid.Empty)
        {
            return Result.Failure<ProducerReceipt>(ProducerReceiptErrors.SaleRequired);
        }

        if (producerPartyId == Guid.Empty)
        {
            return Result.Failure<ProducerReceipt>(ProducerReceiptErrors.ProducerRequired);
        }

        if (grossAmount <= 0m)
        {
            return Result.Failure<ProducerReceipt>(ProducerReceiptErrors.NonPositiveGross);
        }

        if (agriWithholdingAmount < 0m || farmerSskAmount < 0m)
        {
            return Result.Failure<ProducerReceipt>(ProducerReceiptErrors.NegativeDeduction);
        }

        var gross = Money.RoundToKurus(grossAmount);
        var agri = Money.RoundToKurus(agriWithholdingAmount);
        var ssk = Money.RoundToKurus(farmerSskAmount);
        var net = Money.RoundToKurus(gross - agri - ssk);

        if (net < 0m)
        {
            // e-MM'deki kesintiler brütü aşamaz (değişmez, BK-1) — bozuk event'e karşı koruma.
            return Result.Failure<ProducerReceipt>(ProducerReceiptErrors.NegativeNet);
        }

        return new ProducerReceipt(
            Guid.NewGuid(),
            tenantId,
            saleTransactionId,
            producerPartyId,
            buyerPartyId,
            issueDate,
            gross,
            agri,
            ssk,
            net);
    }

    /// <summary>
    /// Belgeyi GİB'e gönderilmiş/kesilmiş olarak işaretler ve makbuz numarasını atar (docs/02 §3.5
    /// <c>DocumentIssued</c>; docs/04 ADR-007 gönderim). <see cref="ProducerReceiptIssued"/> event'i
    /// yayınlanır (outbox'a atomik — docs/04 §10). Yalnız Draft/Failed durumundan Issued'a geçilebilir;
    /// tekrar çağrı (idempotent) zararsızdır (zaten Issued ise event tekrar üretilmez). Makbuz numarası
    /// zorunludur.
    /// </summary>
    public Result MarkIssued(string receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            return Result.Failure(ProducerReceiptErrors.ReceiptNumberRequired);
        }

        if (Status == ProducerReceiptStatus.Cancelled)
        {
            return Result.Failure(ProducerReceiptErrors.CancelledCannotIssue);
        }

        if (Status == ProducerReceiptStatus.Issued)
        {
            // Zaten kesilmiş — idempotent tekrar; yeni event üretme (docs/04 §5).
            return Result.Success();
        }

        ReceiptNumber = receiptNumber.Trim();
        Status = ProducerReceiptStatus.Issued;

        RaiseDomainEvent(new ProducerReceiptIssued(
            Id,
            TenantId,
            SaleTransactionId,
            ProducerPartyId,
            ReceiptNumber,
            NetPayable,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Belgeyi gönderim başarısız olarak işaretler (docs/02 §3.5 <c>DocumentRejected</c>; docs/03 BK-4).
    /// Kullanıcı uyarılır; ADR-007 retry ile yeniden denenebilir. İptal edilmiş belge için geçersizdir.
    /// </summary>
    public Result MarkFailed()
    {
        if (Status == ProducerReceiptStatus.Cancelled)
        {
            return Result.Failure(ProducerReceiptErrors.CancelledCannotIssue);
        }

        Status = ProducerReceiptStatus.Failed;
        return Result.Success();
    }

    /// <summary>e-MM domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
    public static class ProducerReceiptErrors
    {
        public static readonly Error SaleRequired =
            new("ProducerReceipt.SaleRequired", "e-Müstahsil Makbuzu için satış referansı zorunludur.");

        public static readonly Error ProducerRequired =
            new("ProducerReceipt.ProducerRequired", "e-Müstahsil Makbuzu için müstahsil referansı zorunludur.");

        public static readonly Error NonPositiveGross =
            new("ProducerReceipt.NonPositiveGross", "Brüt tutar sıfırdan büyük olmalıdır.");

        public static readonly Error NegativeDeduction =
            new("ProducerReceipt.NegativeDeduction", "Kesinti tutarları negatif olamaz.");

        public static readonly Error NegativeNet =
            new("ProducerReceipt.NegativeNet", "Müstahsile ödenecek net (brüt − stopaj − Bağ-Kur) negatif olamaz.");

        public static readonly Error ReceiptNumberRequired =
            new("ProducerReceipt.ReceiptNumberRequired", "Makbuz numarası zorunludur.");

        public static readonly Error CancelledCannotIssue =
            new("ProducerReceipt.CancelledCannotIssue", "İptal edilmiş makbuz kesilemez.");

        public static readonly Error NotFound =
            new("ProducerReceipt.NotFound", "e-Müstahsil Makbuzu bulunamadı.");
    }
}
