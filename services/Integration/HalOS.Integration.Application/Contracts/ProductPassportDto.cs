using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Contracts;

/// <summary>
/// Künye (ProductPassport) okuma DTO'su (docs/02 §3.5). HKS 19-haneli kod QR ile sorgulanır. Domain
/// aggregate'i API'ye sızmaz.
/// </summary>
public sealed record ProductPassportDto(
    Guid Id,
    Guid TenantId,
    Guid ConsignmentId,
    Guid ConsignmentItemId,
    Guid ProductId,
    Guid ProducerPartyId,
    decimal Quantity,
    string UnitCode,
    DateTime ReceivedAt,
    string? PassportCode,
    ProductPassportStatus Status)
{
    public static ProductPassportDto FromDomain(ProductPassport passport) => new(
        passport.Id,
        passport.TenantId,
        passport.ConsignmentId,
        passport.ConsignmentItemId,
        passport.ProductId,
        passport.ProducerPartyId,
        passport.Quantity,
        passport.UnitCode,
        passport.ReceivedAt,
        passport.PassportCode,
        passport.Status);
}
