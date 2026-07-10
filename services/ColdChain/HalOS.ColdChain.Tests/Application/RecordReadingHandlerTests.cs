using FluentAssertions;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Features.RecordReading;
using HalOS.ColdChain.Domain.Aggregates;
using Moq;
using Xunit;

namespace HalOS.ColdChain.Tests.Application;

public sealed class RecordReadingHandlerTests
{
    private readonly Mock<IColdStorageUnitRepository> _units = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RecordReadingHandler CreateHandler() => new(_units.Object, _unitOfWork.Object);

    private static ColdStorageUnit NewUnit() =>
        ColdStorageUnit.Register(Guid.NewGuid(), "Oda", 0m, 4m).Value;

    [Fact]
    public async Task Handle_Valid_RecordsReading_AndSaves()
    {
        var unit = NewUnit();
        _units.Setup(r => r.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        var result = await CreateHandler().Handle(
            new RecordReadingCommand(unit.Id, Guid.NewGuid(), 2m, null, DateTime.UtcNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        unit.Readings.Should().HaveCount(1);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnitNotFound_Fails_DoesNotSave()
    {
        _units.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ColdStorageUnit?)null);

        var result = await CreateHandler().Handle(
            new RecordReadingCommand(Guid.NewGuid(), Guid.NewGuid(), 2m, null, DateTime.UtcNow),
            CancellationToken.None);

        result.Error.Should().Be(ColdStorageUnitErrors.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BreachReading_RaisesEventOnAggregate()
    {
        var unit = NewUnit();
        _units.Setup(r => r.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        await CreateHandler().Handle(
            new RecordReadingCommand(unit.Id, Guid.NewGuid(), 9m, null, DateTime.UtcNow),
            CancellationToken.None);

        // Alarm event'i aggregate'te birikir; DbContext.SaveChanges outbox'a yazar (docs/04 §10).
        unit.DomainEvents.Should().ContainSingle();
    }
}
