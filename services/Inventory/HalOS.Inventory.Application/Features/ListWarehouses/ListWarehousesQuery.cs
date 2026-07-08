using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListWarehouses;

/// <summary>
/// Tenant'ın depolarını ada göre sıralı listeler (docs/06 S2.1 depo lokasyonu). Tenant global filter
/// otomatik uygulanır (BK-8).
/// </summary>
public sealed record ListWarehousesQuery : IQuery<IReadOnlyList<WarehouseDto>>;
