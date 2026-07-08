using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// GİB/HKS e-belge (e-Müstahsil Makbuzu / e-Fatura HAL / HKS bildirimi) gönderim soyutlaması (docs/04
/// ADR-007/ADR-010). Dış (GİB/HKS) çağrısı kırılgandır; ADR-007 gereği retry + outbox ile yalıtılır. Bu
/// slice'ta Infrastructure'da STUB uygulanır (sahte belge/referans numarası üretir, başarılı döner);
/// gerçek GİB e-Fatura + HKS sandbox entegrasyonu SONRAKİ slice'ta gelir.
///
/// <para>
/// Gerçek uygulamada gönderim HTTP çağrısıdır; docs/07 §5 gereği handler/consumer içinde doğrudan
/// dış çağrı yapılmaz — nihai tasarımda gönderim outbox tetikli olacaktır (ADR-007). Bu STUB
/// senkron döndüğünden ve dış G/Ç yapmadığından consumer güvenle çağırabilir; gerçek entegrasyona
/// geçişte gönderim ayrı bir outbox worker'ına taşınacaktır (notes).
/// </para>
/// </summary>
public interface IEDocumentGateway
{
    /// <summary>
    /// Verilen e-MM belgesini GİB'e gönderir/keser. Başarılıysa atanan makbuz numarasını taşıyan
    /// <see cref="Result{T}"/> döner; başarısızsa anlamlı bir <see cref="Error"/> (docs/07 §10) —
    /// consumer bunu yutmadan istisnaya çevirir (retry/error queue, docs/04 §10).
    /// </summary>
    Task<Result<string>> SendProducerReceiptAsync(ProducerReceipt receipt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen e-Fatura (HAL) belgesini GİB'e gönderir/keser. Başarılıysa atanan fatura numarasını
    /// taşıyan <see cref="Result{T}"/> döner; başarısızsa anlamlı bir <see cref="Error"/> — consumer
    /// bunu yutmadan istisnaya çevirir (retry/error queue, docs/04 §10). e-MM deseniyle birebir.
    /// </summary>
    Task<Result<string>> SendInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen satışı HKS'e bildirir. Başarılıysa HKS referans numarasını taşıyan
    /// <see cref="Result{T}"/> döner; başarısızsa anlamlı bir <see cref="Error"/> — consumer bunu
    /// yutmadan istisnaya çevirir (retry/error queue, docs/04 §10). e-MM deseniyle birebir.
    /// </summary>
    Task<Result<string>> SendHksNotificationAsync(HksNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen künye (<see cref="ProductPassport"/>) için HKS'ten 19-haneli künye kodu üretir/tescil eder.
    /// Başarılıysa atanan 19-haneli kodu taşıyan <see cref="Result{T}"/> döner; başarısızsa anlamlı bir
    /// <see cref="Error"/> — consumer bunu yutmadan istisnaya çevirir (retry/error queue, docs/04 §10).
    /// e-MM/e-Fatura/HKS deseniyle birebir. Bu slice'ta STUB.
    /// </summary>
    Task<Result<string>> GenerateProductPassportAsync(ProductPassport passport, CancellationToken cancellationToken = default);
}
