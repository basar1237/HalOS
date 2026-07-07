using HalOS.BuildingBlocks.Application;

namespace HalOS.Integration.Application.Features.ReissueProducerReceipt;

/// <summary>
/// Başarısız (Failed) veya taslak (Draft) bir e-MM'i GİB'e yeniden gönderir/keser (docs/03 §5 e-Belge
/// Merkezi "red yönetimi"; docs/03 BK-4 belge reddi). Yetki: Muhasebe/Yönetici/Patron (docs/03 §3).
/// </summary>
public sealed record ReissueProducerReceiptCommand(Guid ReceiptId) : ICommand;
