using HalOS.BuildingBlocks.Application;
using HalOS.Party.Application.Contracts;

namespace HalOS.Party.Application.Features.GetParty;

/// <summary>Tekil taraf sorgusu (tenant filtreli, BK-8).</summary>
public sealed record GetPartyQuery(Guid PartyId) : IQuery<PartyDto>;
