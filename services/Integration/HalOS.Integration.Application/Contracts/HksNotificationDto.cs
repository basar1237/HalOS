using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Contracts;

/// <summary>
/// HKS Bildirimi (<c>HksNotification</c>) okuma DTO'su (docs/02 §3.5). Brüt + komisyon + hal rüsumu
/// AYRI taşınır (docs/02 §7). Domain aggregate'i API'ye sızmaz.
/// </summary>
public sealed record HksNotificationDto(
    Guid Id,
    Guid TenantId,
    Guid SaleTransactionId,
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    DateTime NotifiedDate,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal MarketFeeAmount,
    string? ReferenceNumber,
    HksNotificationStatus Status)
{
    public static HksNotificationDto FromDomain(HksNotification notification) => new(
        notification.Id,
        notification.TenantId,
        notification.SaleTransactionId,
        notification.BuyerPartyId,
        notification.ProducerPartyId,
        notification.NotifiedDate,
        notification.GrossAmount,
        notification.CommissionAmount,
        notification.MarketFeeAmount,
        notification.ReferenceNumber,
        notification.Status);
}
