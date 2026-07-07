using HalOS.BuildingBlocks.Application;
using HalOS.Party.Domain.Enums;

namespace HalOS.Party.Application.Features.AddPartyRole;

/// <summary>Bir tarafa yeni bir rol ekler (docs/02 §3.1 — bir taraf birden çok rol taşıyabilir).</summary>
public sealed record AddPartyRoleCommand(Guid PartyId, PartyRoleType Type) : ICommand;
