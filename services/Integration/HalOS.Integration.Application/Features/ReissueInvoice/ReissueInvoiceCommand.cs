using HalOS.BuildingBlocks.Application;

namespace HalOS.Integration.Application.Features.ReissueInvoice;

/// <summary>
/// Başarısız (Failed) veya taslak (Draft) bir e-Fatura'yı GİB'e yeniden gönderir/keser (docs/03 §5
/// e-Belge Merkezi "red yönetimi"; docs/03 BK-4 belge reddi). Yetki: Muhasebe/Yönetici/Patron
/// (docs/03 §3). e-MM ReissueProducerReceipt deseniyle birebir.
/// </summary>
public sealed record ReissueInvoiceCommand(Guid InvoiceId) : ICommand;
