using HalOS.BuildingBlocks.Application;
using HalOS.Party.Application.Features.CreateParty;

namespace HalOS.Party.Application.Features.UpdateParty;

/// <summary>Bir tarafın kimlik/iletişim ve stopaj profili alanlarını günceller (docs/03 M1).</summary>
public sealed record UpdatePartyCommand(
    Guid PartyId,
    string DisplayName,
    string? TaxOffice,
    string? Phone,
    string? Address,
    bool KeepsRecords,
    WithholdingProfileInput? WithholdingProfile) : ICommand;
