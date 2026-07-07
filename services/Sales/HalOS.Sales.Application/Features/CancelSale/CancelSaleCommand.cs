using HalOS.BuildingBlocks.Application;

namespace HalOS.Sales.Application.Features.CancelSale;

/// <summary>
/// Satışı iptal eder (docs/03 §4 BK-9). Tamamlanmış satış SİLİNMEZ; durum Cancelled'a çekilir,
/// gerekçe saklanır (denetim izi). SaleCancelled event'i yayınlanır (ters kayıt Finance/e-Belge).
/// </summary>
public sealed record CancelSaleCommand(Guid SaleId, string Reason) : ICommand;
