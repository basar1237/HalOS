using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.ColdChain.Domain.Aggregates;
using Xunit;

namespace HalOS.ColdChain.Tests.Domain;

/// <summary>
/// ColdStorageUnit domain testleri (docs/04 §6). Eşik değerlendirmesi, idempotency ve alarm event'i
/// (TemperatureThresholdBreached) davranışı. Saf domain — altyapı yok (docs/07 §7).
/// </summary>
public sealed class ColdStorageUnitTests
{
    private static ColdStorageUnit NewUnit(decimal min = 0m, decimal max = 4m) =>
        ColdStorageUnit.Register(Guid.NewGuid(), "1 No'lu Soğuk Oda", min, max).Value;

    [Fact]
    public void Register_InvalidRange_Fails()
    {
        var result = ColdStorageUnit.Register(Guid.NewGuid(), "Oda", 5m, 4m);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ColdStorageUnitErrors.InvalidThresholdRange);
    }

    [Fact]
    public void Register_EmptyName_Fails()
    {
        var result = ColdStorageUnit.Register(Guid.NewGuid(), "  ", 0m, 4m);
        result.Error.Should().Be(ColdStorageUnitErrors.NameRequired);
    }

    [Fact]
    public void RecordReading_WithinRange_NoAlarm()
    {
        var unit = NewUnit(0m, 4m);
        var result = unit.RecordReading(Guid.NewGuid(), 2.5m, humidityPercent: null, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        unit.Readings.Should().HaveCount(1);
        unit.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordReading_AboveMax_RaisesBreach_AboveMaxTrue()
    {
        var unit = NewUnit(0m, 4m);
        unit.RecordReading(Guid.NewGuid(), 7.2m, null, DateTime.UtcNow);

        unit.DomainEvents.Should().ContainSingle();
        var breach = unit.DomainEvents.OfType<TemperatureThresholdBreached>().Single();
        breach.AboveMax.Should().BeTrue();
        breach.TemperatureC.Should().Be(7.2m);
        breach.MaxTempC.Should().Be(4m);
    }

    [Fact]
    public void RecordReading_BelowMin_RaisesBreach_AboveMaxFalse()
    {
        var unit = NewUnit(0m, 4m);
        unit.RecordReading(Guid.NewGuid(), -3m, null, DateTime.UtcNow);

        var breach = unit.DomainEvents.OfType<TemperatureThresholdBreached>().Single();
        breach.AboveMax.Should().BeFalse();
    }

    [Fact]
    public void RecordReading_DuplicateReadingId_Idempotent_NoSecondReading()
    {
        var unit = NewUnit();
        var readingId = Guid.NewGuid();
        unit.RecordReading(readingId, 2m, null, DateTime.UtcNow);
        var second = unit.RecordReading(readingId, 2m, null, DateTime.UtcNow);

        second.IsSuccess.Should().BeTrue();
        unit.Readings.Should().HaveCount(1);
    }

    [Fact]
    public void RecordReading_InvalidHumidity_Fails()
    {
        var unit = NewUnit();
        var result = unit.RecordReading(Guid.NewGuid(), 2m, humidityPercent: 150m, DateTime.UtcNow);
        result.Error.Should().Be(ColdStorageUnitErrors.InvalidHumidity);
    }

    [Fact]
    public void RecordReading_Inactive_Fails()
    {
        var unit = NewUnit();
        unit.Deactivate();
        var result = unit.RecordReading(Guid.NewGuid(), 2m, null, DateTime.UtcNow);
        result.Error.Should().Be(ColdStorageUnitErrors.Inactive);
    }

    [Fact]
    public void LatestReading_ReturnsMostRecentByOccurredAt()
    {
        var unit = NewUnit();
        var t0 = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        unit.RecordReading(Guid.NewGuid(), 1m, null, t0);
        unit.RecordReading(Guid.NewGuid(), 3m, null, t0.AddMinutes(5));

        unit.LatestReading!.TemperatureC.Should().Be(3m);
    }

    [Fact]
    public void UpdateThresholds_InvalidRange_Fails()
    {
        var unit = NewUnit();
        unit.UpdateThresholds(10m, 2m).Error.Should().Be(ColdStorageUnitErrors.InvalidThresholdRange);
    }
}
