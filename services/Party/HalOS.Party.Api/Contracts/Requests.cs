using HalOS.Party.Application.Features.CreateParty;
using HalOS.Party.Domain.Enums;

namespace HalOS.Party.Api.Contracts;

/// <summary>Yeni taraf oluşturma isteği (docs/03 M1).</summary>
public sealed record CreatePartyRequest(
    string DisplayName,
    string? Tckn,
    string? Vkn,
    string? TaxOffice,
    string? Phone,
    string? Address,
    bool KeepsRecords,
    WithholdingProfileInput? WithholdingProfile,
    IReadOnlyList<PartyRoleType> Roles);

/// <summary>Taraf güncelleme isteği.</summary>
public sealed record UpdatePartyRequest(
    string DisplayName,
    string? TaxOffice,
    string? Phone,
    string? Address,
    bool KeepsRecords,
    WithholdingProfileInput? WithholdingProfile);

/// <summary>Tarafa rol ekleme isteği.</summary>
public sealed record AddPartyRoleRequest(PartyRoleType Type);
