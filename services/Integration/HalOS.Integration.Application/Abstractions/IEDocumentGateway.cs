using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// GİB e-belge (bu slice'ta e-Müstahsil Makbuzu / e-MM) gönderim soyutlaması (docs/04 ADR-007/ADR-010).
/// Dış (GİB) çağrısı kırılgandır; ADR-007 gereği retry + outbox ile yalıtılır. Bu slice'ta
/// Infrastructure'da STUB uygulanır (sahte makbuz numarası üretir, başarılı döner); gerçek GİB e-MM
/// sandbox entegrasyonu SONRAKİ slice'ta gelir.
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
}
