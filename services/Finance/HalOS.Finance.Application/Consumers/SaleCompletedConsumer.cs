using HalOS.BuildingBlocks.Contracts;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Finance.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'i tüketip cariyi günceller (docs/02 §5/§6:
/// SaleCompleted → Finans cari). İki cari hareketi atomik yazar (docs/04 §10 en-az-bir-kez teslimat):
/// <list type="bullet">
///   <item>Alıcı carisine BORÇ: alıcının ödeyeceği brüt tutar (docs/02 §5: alıcı cari borç).</item>
///   <item>Müstahsil carisine ALACAK: net hakediş + ödeme planı vade tarihi (BK-3; vade
///     SaleCompleted.SettlementDueDate — normal satış 15 iş günü). PaymentDue event'i yayınlanır.</item>
/// </list>
///
/// <b>Idempotency</b> (docs/04 §5): her iki cari de <c>SaleTransactionId</c> üzerinden çift-kayıt
/// korumalıdır (<see cref="CurrentAccount.IsSaleAlreadyRecorded"/>); aynı satış iki kez gelse
/// (broker retry) ikinci kez hareket eklenmez. Cari hesaplar yoksa açılır (upsert).
///
/// <b>Tenant</b>: broker mesajında HTTP/JWT bağlamı olmadığından tenant, event'in kendisiyle
/// (<see cref="ITenantScopedEvent"/>) taşınır ve <c>TenantConsumeFilter</c> ile ambient tenant'a
/// set edilir; böylece repository/DbContext global query filter'ı DOĞRU tenant'ta çalışır ve
/// SaveChanges o tenant kapsamında izole kalır (docs/07 §6 / BK-8). El-yapımı outbox korunur.
/// </summary>
public sealed class SaleCompletedConsumer : IConsumer<SaleCompleted>
{
    private readonly ICurrentAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(
        ICurrentAccountRepository accounts,
        IUnitOfWork unitOfWork,
        ILogger<SaleCompletedConsumer> logger)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // Bu Consume çağrısı içinde açılan/getirilen hesapları izler. Alıcı == müstahsil kenar
        // durumunda ikinci GetOrOpenAsync, henüz kalıcılaştırılmamış AYNI hesabı yeniden kullanır;
        // aksi halde iki ayrı hesap açılıp unique (tenant_id, party_id) ihlali oluşur.
        var openedAccounts = new Dictionary<Guid, CurrentAccount>();

        // --- Alıcı carisi: satış BORCU (alıcının ödeyeceği brüt) ---
        var buyerAccount = await GetOrOpenAsync(openedAccounts, message.TenantId, message.BuyerPartyId, ct);
        var debitResult = buyerAccount.RecordSaleDebit(message.SaleTransactionId, message.GrossAmount, message.SoldAt);

        // --- Müstahsil carisi: net hakediş ALACAK + ödeme planı vade tarihi (BK-3) ---
        // Alıcı == müstahsil kenar durumunda GetOrOpenAsync aynı hesabı (change-tracker'dan) döndürür;
        // böylece tek satış iki ayrı hesap açıp unique (tenant_id, party_id) ihlaline yol açmaz.
        var producerAccount = await GetOrOpenAsync(openedAccounts, message.TenantId, message.ProducerPartyId, ct);
        var creditResult = producerAccount.RecordSettlementCredit(
            message.SaleTransactionId,
            message.NetAmount,
            message.SettlementDueDate,
            message.SoldAt);

        // Broker'dan gelen SaleCompleted güvenilmez sınırdır: bozuk/kötücül bir mesaj (örn. brüt<=0
        // ama net>0) tek-taraflı yazımla çift-kayıt değişmezini bozabilir (docs/02 §5 borç/alacak).
        // İki mutasyonun ikisi de tek SaveChanges'ten ÖNCE doğrulanmalı; herhangi biri başarısızsa
        // HİÇBİR şey kalıcılaştırılmaz ve istisna fırlatılır → MassTransit retry/error queue devreye
        // girer (docs/04 §10 en-az-bir-kez), veri sessizce yarım yazılmaz/ack'lenmez (BK-1).
        if (debitResult.IsFailure || creditResult.IsFailure)
        {
            var error = debitResult.IsFailure ? debitResult.Error : creditResult.Error;
            _logger.LogError(
                "SaleCompleted reddedildi (kısmi cari yazımı engellendi): Tenant={TenantId} " +
                "Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                error.Code,
                error.Message);

            throw new InvalidOperationException(
                $"SaleCompleted {message.SaleTransactionId} reddedildi: {error}");
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Satış cariye işlendi: Tenant={TenantId} Sale={SaleTransactionId} " +
            "AlıcıBorç={GrossAmount} MüstahsilAlacak={NetAmount} Vade={SettlementDueDate:yyyy-MM-dd}.",
            message.TenantId,
            message.SaleTransactionId,
            message.GrossAmount,
            message.NetAmount,
            message.SettlementDueDate);
    }

    /// <summary>
    /// Tarafın cari hesabını getirir; yoksa açar ve repository'ye ekler (upsert). Aynı Consume
    /// çağrısında zaten açılmış/getirilmiş bir hesap varsa (alıcı == müstahsil), onu yeniden
    /// kullanır — yeni satır açıp unique (tenant_id, party_id) ihlaline yol açmaz.
    /// </summary>
    private async Task<CurrentAccount> GetOrOpenAsync(
        Dictionary<Guid, CurrentAccount> openedAccounts,
        Guid tenantId,
        Guid partyId,
        CancellationToken ct)
    {
        if (openedAccounts.TryGetValue(partyId, out var tracked))
        {
            return tracked;
        }

        var account = await _accounts.GetByPartyIdAsync(partyId, ct);
        if (account is null)
        {
            // Open, tenant'ı parametre alır; ambient tenant SaveChanges'te de aynı değeri uygular (BK-8).
            account = CurrentAccount.Open(tenantId, partyId).Value;
            _accounts.Add(account);
        }

        openedAccounts[partyId] = account;
        return account;
    }
}
