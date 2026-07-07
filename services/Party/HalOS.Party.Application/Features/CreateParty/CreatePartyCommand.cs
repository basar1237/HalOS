using HalOS.BuildingBlocks.Application;
using HalOS.Party.Domain.Enums;

namespace HalOS.Party.Application.Features.CreateParty;

/// <summary>Stopaj profili girdi modeli (müstahsile özel override, docs/02 §3.1).</summary>
public sealed record WithholdingProfileInput(decimal AgriWithholdingRate, decimal FarmerSskRate);

/// <summary>
/// Yeni bir taraf (cari kart) oluşturur (docs/03 M1). En az bir rol zorunlu; Producer rolü
/// varsa stopaj profili zorunlu (docs/02 §3.1).
/// </summary>
public sealed record CreatePartyCommand(
    string DisplayName,
    string? Tckn,
    string? Vkn,
    string? TaxOffice,
    string? Phone,
    string? Address,
    bool KeepsRecords,
    WithholdingProfileInput? WithholdingProfile,
    IReadOnlyList<PartyRoleType> Roles) : ICommand<Guid>;
