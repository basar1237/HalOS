using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Features.CompleteSale;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using Moq;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// CompleteSaleHandler unit testleri (docs/03 M5). Motoru IRateProvider'dan çözülen oranlarla
/// çalıştırır; Complete başarılıysa Update + SaveChanges çağrılır. Mock repo/port ile (docs/07 §7).
/// </summary>
public sealed class CompleteSaleHandlerTests
{
    private readonly Mock<ISaleTransactionRepository> _sales = new();
    private readonly Mock<IRateProvider> _rateProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CompleteSaleHandler CreateHandler() =>
        new(_sales.Object, _rateProvider.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    private SaleTransaction DraftWithLine(decimal gross)
    {
        var sale = SaleTransaction.Create(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null,
            new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: Guid.NewGuid()).Value;
        sale.AddLine(Guid.NewGuid(), gross, UnitOfMeasure.Kilogram, 1m);
        return sale;
    }

    [Fact]
    public async Task Handle_ValidSale_RunsEngine_CompletesAndSaves()
    {
        var sale = DraftWithLine(100m);
        _sales.Setup(r => r.GetByIdAsync(sale.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sale);
        _rateProvider
            .Setup(r => r.ResolveAsync(_tenantId, sale.ProducerPartyId, sale.SoldAt, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m));

        var result = await CreateHandler().Handle(new CompleteSaleCommand(sale.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Completed);
        sale.Settlement!.NetAmount.Should().Be(88.00m);
        _sales.Verify(r => r.Update(sale), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaleNotFound_Fails()
    {
        var id = Guid.NewGuid();
        _sales.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((SaleTransaction?)null);

        var result = await CreateHandler().Handle(new CompleteSaleCommand(id), CancellationToken.None);

        result.Error.Should().Be(SaleErrors.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RateProviderFails_PropagatesError_DoesNotSave()
    {
        var sale = DraftWithLine(100m);
        _sales.Setup(r => r.GetByIdAsync(sale.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sale);
        _rateProvider
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RateSet>(RateSetErrors.CommissionRateTooHigh));

        var result = await CreateHandler().Handle(new CompleteSaleCommand(sale.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RateSetErrors.CommissionRateTooHigh);
        sale.Status.Should().Be(SaleStatus.Draft);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
