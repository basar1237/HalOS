using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.CompleteSale;

/// <summary>
/// Satışı tamamlayan handler (docs/03 M5). Oranları satış anında <see cref="IRateProvider"/>
/// ile çözer (tenant + tarih + taraf — docs/02 §4), motoru <c>SaleTransaction.Complete</c> ile
/// çalıştırır. Complete içinde CommissionCalculation + Deduction'lar + Settlement üretilir ve
/// SaleCompleted event'i eklenir; SaveChanges'te outbox'a atomik yazılır (docs/04 §10).
/// </summary>
internal sealed class CompleteSaleHandler : ICommandHandler<CompleteSaleCommand>
{
    private readonly ISaleTransactionRepository _sales;
    private readonly IRateProvider _rateProvider;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSaleHandler(
        ISaleTransactionRepository sales,
        IRateProvider rateProvider,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _sales = sales;
        _rateProvider = rateProvider;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CompleteSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _sales.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure(SaleErrors.NotFound);
        }

        var rateResult = await _rateProvider.ResolveAsync(
            _tenantContext.TenantId,
            sale.ProducerPartyId,
            sale.SoldAt,
            sale.IsWithinMarket,
            cancellationToken);

        if (rateResult.IsFailure)
        {
            return Result.Failure(rateResult.Error);
        }

        var completeResult = sale.Complete(rateResult.Value);
        if (completeResult.IsFailure)
        {
            return completeResult;
        }

        // sale İZLENEN (tracked); Complete kökü değiştirir (Status/GrossAmount → change tracking
        // algılar) ve yeni bağlı entity'ler üretir: Deduction'lar + CommissionCalculation +
        // Settlement. Bunlar client-generated Guid ID taşıdığından EF yanlışlıkla "Modified" sanar
        // (UPDATE → 0 satır → hata); bu yüzden her birini açıkça "Added" olarak bildiririz.
        foreach (var deduction in sale.Deductions)
        {
            _sales.RegisterNew(deduction);
        }

        _sales.RegisterNew(sale.CommissionCalculation!);
        _sales.RegisterNew(sale.Settlement!);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
