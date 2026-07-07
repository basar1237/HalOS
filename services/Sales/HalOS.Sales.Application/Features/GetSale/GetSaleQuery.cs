using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.GetSale;

/// <summary>Tekil satış kaydını satırları/kesinti/hakedişiyle getirir (docs/03 M4/M5).</summary>
public sealed record GetSaleQuery(Guid SaleId) : IQuery<SaleDto>;
