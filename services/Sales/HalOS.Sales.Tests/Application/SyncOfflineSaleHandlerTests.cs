using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Features.SyncOfflineSale;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using Moq;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// SyncOfflineSaleHandler unit testleri (docs/04 §5, ADR-005). Terminal offline satışını TEK
/// idempotent çağrıda oynatır: create + satırlar + complete, tek SaveChanges. operationId ile
/// çift senkron güvenlidir. Mock repo/port ile (docs/07 §7).
/// </summary>
public sealed class SyncOfflineSaleHandlerTests
{
    private readonly Mock<ISaleTransactionRepository> _sales = new();
    private readonly Mock<IRateProvider> _rateProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private SyncOfflineSaleHandler CreateHandler() =>
        new(
            _sales.Object,
            _rateProvider.Object,
            new StubTenantContext(_tenantId),
            new StubCurrentUserContext(_userId),
            _unitOfWork.Object);

    private SyncOfflineSaleCommand Command(Guid operationId, params OfflineSaleLine[] lines) =>
        new(
            BuyerPartyId: Guid.NewGuid(),
            ProducerPartyId: Guid.NewGuid(),
            ConsignmentId: null,
            SoldAt: new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            IsWithinMarket: true,
            OperationId: operationId,
            Term: SaleTerm.Cash,
            Lines: lines.Length == 0
                ? new[] { new OfflineSaleLine(Guid.NewGuid(), 100m, UnitOfMeasure.Kilogram, 1m) }
                : lines);

    [Fact]
    public async Task Handle_NewOperation_CreatesCompletesAndSaves_Once()
    {
        var operationId = Guid.NewGuid();
        _sales.Setup(r => r.GetByOperationIdAsync(operationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaleTransaction?)null);
        _rateProvider
            .Setup(r => r.ResolveAsync(_tenantId, It.IsAny<Guid>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m));

        SaleTransaction? added = null;
        _sales.Setup(r => r.Add(It.IsAny<SaleTransaction>()))
            .Callback<SaleTransaction>(s => added = s);

        var result = await CreateHandler().Handle(Command(operationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.TenantId.Should().Be(_tenantId);
        added.Status.Should().Be(SaleStatus.Completed);
        added.GrossAmount.Should().Be(100m);
        added.Settlement!.NetAmount.Should().Be(88.00m); // BK: net 88 (docs/03 §4)
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateOperationId_ReturnsExisting_DoesNotAddOrSave()
    {
        var operationId = Guid.NewGuid();
        var existing = SaleTransaction.Create(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow, true, operationId, _userId).Value;
        _sales.Setup(r => r.GetByOperationIdAsync(operationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(Command(operationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        _sales.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RateProviderFails_PropagatesError_DoesNotSave()
    {
        _sales.Setup(r => r.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaleTransaction?)null);
        _rateProvider
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RateSet>(RateSetErrors.CommissionRateTooHigh));

        var result = await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RateSetErrors.CommissionRateTooHigh);
        _sales.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
