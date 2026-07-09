namespace HalOS.Integration.Application.Contracts;

/// <summary>
/// Bekleyen e-belge özeti (dashboard "Bekleyen e-Belge" kartı). İşlem bekleyen = Draft (henüz
/// gönderilmedi) veya Failed (gönderim/red başarısız, yeniden denenebilir) durumundaki belgeler.
/// SALT-OKUMA CQRS; tenant filtreli (BK-8).
/// </summary>
/// <param name="PendingInvoices">Bekleyen e-Fatura adedi.</param>
/// <param name="PendingProducerReceipts">Bekleyen e-Müstahsil Makbuzu adedi.</param>
/// <param name="PendingHksNotifications">Bekleyen HKS bildirimi adedi.</param>
/// <param name="Total">Toplam bekleyen belge adedi.</param>
public sealed record PendingDocumentsDto(
    long PendingInvoices,
    long PendingProducerReceipts,
    long PendingHksNotifications,
    long Total);
