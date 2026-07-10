using HalOS.BuildingBlocks.Application;
using HalOS.ColdChain.Application.Contracts;

namespace HalOS.ColdChain.Application.Features.GetUnit;

/// <summary>Tek bir soğuk hava deposunu (son okuma özetiyle) getirir (docs/04 §6).</summary>
public sealed record GetUnitQuery(Guid Id) : IQuery<ColdStorageUnitDto>;
