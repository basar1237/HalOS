using HalOS.Party.Domain.Enums;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Application.Contracts;

/// <summary>Stopaj/Bağ-Kur oran profili okuma DTO'su (docs/02 §3.1).</summary>
public sealed record WithholdingProfileDto(decimal AgriWithholdingRate, decimal FarmerSskRate);

/// <summary>Taraf (cari kart) okuma DTO'su. Domain aggregate'i API'ye sızmaz.</summary>
public sealed record PartyDto(
    Guid Id,
    Guid TenantId,
    string DisplayName,
    string? Tckn,
    string? Vkn,
    string? TaxOffice,
    string? Phone,
    string? Address,
    bool KeepsRecords,
    WithholdingProfileDto? WithholdingProfile,
    bool IsActive,
    DateTime CreatedOnUtc,
    IReadOnlyList<PartyRoleType> Roles)
{
    public static PartyDto FromDomain(PartyAggregate party) => new(
        party.Id,
        party.TenantId,
        party.DisplayName,
        party.Tckn,
        party.Vkn,
        party.TaxOffice,
        party.Phone,
        party.Address,
        party.KeepsRecords,
        party.WithholdingProfile is null
            ? null
            : new WithholdingProfileDto(
                party.WithholdingProfile.AgriWithholdingRate,
                party.WithholdingProfile.FarmerSskRate),
        party.IsActive,
        party.CreatedOnUtc,
        party.Roles.Select(r => r.Type).ToList());
}
