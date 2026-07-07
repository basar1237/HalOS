using HalOS.BuildingBlocks.Application;

namespace HalOS.Party.Application.Features.DeactivateParty;

/// <summary>Bir tarafı pasifleştirir (master veri soft-delete, docs/05 §1).</summary>
public sealed record DeactivatePartyCommand(Guid PartyId) : ICommand;
