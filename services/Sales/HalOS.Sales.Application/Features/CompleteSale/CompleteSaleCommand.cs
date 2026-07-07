using HalOS.BuildingBlocks.Application;

namespace HalOS.Sales.Application.Features.CompleteSale;

/// <summary>
/// Satışı tamamlar ve kesinti/hakediş motorunu çalıştırır (docs/03 M5; docs/02 §4; BK-1/BK-2/BK-3).
/// Oranlar <c>IRateProvider</c> ile satış anında çözülür ve <c>SaleTransaction.Complete</c>'e verilir.
/// </summary>
public sealed record CompleteSaleCommand(Guid SaleId) : ICommand;
