using FluentAssertions;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Features.CreateSale;
using HalOS.Sales.Domain.Aggregates;
using Moq;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// CreateSaleHandler unit testleri (docs/03 M4). Tenant bağlamdan alınır; offline idempotency
/// (docs/04 §5): aynı operationId ile mevcut satış döndürülür, yeni kayıt açılmaz.
/// </summary>
public sealed class CreateSaleHandlerTests
{
    private readonly Mock<ISaleTransactionRepository> _sales = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private CreateSaleHandler CreateHandler() =>
        new(_sales.Object, new StubTenantContext(_tenantId), new StubCurrentUserContext(_userId), _unitOfWork.Object);

    [Fact]
    public async Task Handle_NewOperation_CreatesSaleWithTenantFromContext()
    {
        var operationId = Guid.NewGuid();
        _sales.Setup(r => r.GetByOperationIdAsync(operationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaleTransaction?)null);

        var command = new CreateSaleCommand(
            Guid.NewGuid(), Guid.NewGuid(), null,
            new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc), IsWithinMarket: true, operationId);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _sales.Verify(r => r.Add(It.Is<SaleTransaction>(s => s.TenantId == _tenantId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateOperationId_ReturnsExisting_DoesNotAdd()
    {
        var operationId = Guid.NewGuid();
        var existing = SaleTransaction.Create(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow, true, operationId, _userId).Value;
        _sales.Setup(r => r.GetByOperationIdAsync(operationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new CreateSaleCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow, IsWithinMarket: true, operationId);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        _sales.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
