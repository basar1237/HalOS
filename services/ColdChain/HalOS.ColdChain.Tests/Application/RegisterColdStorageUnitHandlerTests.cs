using FluentAssertions;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Features.RegisterColdStorageUnit;
using HalOS.ColdChain.Domain.Aggregates;
using Moq;
using Xunit;

namespace HalOS.ColdChain.Tests.Application;

public sealed class RegisterColdStorageUnitHandlerTests
{
    private readonly Mock<IColdStorageUnitRepository> _units = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private RegisterColdStorageUnitHandler CreateHandler() =>
        new(_units.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valid_AddsUnitWithTenant_AndSaves()
    {
        ColdStorageUnit? added = null;
        _units.Setup(r => r.Add(It.IsAny<ColdStorageUnit>())).Callback<ColdStorageUnit>(u => added = u);

        var result = await CreateHandler().Handle(
            new RegisterColdStorageUnitCommand("Soğuk Oda 1", 0m, 4m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.TenantId.Should().Be(_tenantId);
        added.MinTempC.Should().Be(0m);
        added.MaxTempC.Should().Be(4m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRange_Fails_DoesNotSave()
    {
        var result = await CreateHandler().Handle(
            new RegisterColdStorageUnitCommand("Oda", 5m, 4m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _units.Verify(r => r.Add(It.IsAny<ColdStorageUnit>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
