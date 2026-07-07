using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using Xunit;

namespace HalOS.Sales.Tests.Domain;

/// <summary>
/// İş günü hesaplayıcı testleri (docs/03 §4 BK-3). 15 iş günü; hafta sonu VE sabit Türk resmi
/// tatilleri atlanır (docs/05). Hareketli dini bayramlar enjekte edilebilir ek tatil kümesiyle
/// doğrulanır. Saf, in-memory. Hesaplayıcı BuildingBlocks.Domain'e taşındı.
/// </summary>
public sealed class BusinessDayCalculatorTests
{
    [Fact]
    public void AddBusinessDays_SkipsWeekendsAndPublicHolidays_FifteenBusinessDays()
    {
        // 2026-07-06 Pazartesi + 15 iş günü. Aralıkta 15 Temmuz (Demokrasi ve Millî Birlik Günü,
        // Çarşamba) sabit resmi tatildir ve atlanır → sonuç bir iş günü kayar: 2026-07-28 Salı.
        var start = new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc); // Monday

        var due = BusinessDayCalculator.AddBusinessDays(start, 15);

        due.Date.Should().Be(new DateTime(2026, 7, 28));
        due.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    [Fact]
    public void AddBusinessDays_FromFriday_SkipsWeekend()
    {
        // Cuma + 1 iş günü → Pazartesi (Cumartesi/Pazar atlanır). Bu aralıkta tatil yok.
        var friday = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc); // Friday

        var next = BusinessDayCalculator.AddBusinessDays(friday, 1);

        next.DayOfWeek.Should().Be(DayOfWeek.Monday);
        next.Date.Should().Be(new DateTime(2026, 7, 13));
    }

    [Fact]
    public void AddBusinessDays_SkipsFixedPublicHoliday()
    {
        // 28 Ekim Çarşamba + 1 iş günü. 29 Ekim (Cumhuriyet Bayramı, Perşembe) sabit tatildir ve
        // atlanır → 30 Ekim Cuma.
        var beforeRepublicDay = new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc); // Wednesday

        var next = BusinessDayCalculator.AddBusinessDays(beforeRepublicDay, 1);

        next.Date.Should().Be(new DateTime(2026, 10, 30));
        next.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public void AddBusinessDays_SkipsInjectedAdditionalHoliday()
    {
        // Hareketli dini bayram enjekte edildiğinde atlanmalı. 8 Temmuz 2026 Çarşamba'yı ek tatil
        // olarak enjekte et: 7 Temmuz Salı + 1 iş günü normalde 8 Temmuz, ama ek tatil → 9 Temmuz.
        var start = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc); // Tuesday
        var additional = new HashSet<DateOnly> { new(2026, 7, 8) };

        var next = BusinessDayCalculator.AddBusinessDays(start, 1, additional);

        next.Date.Should().Be(new DateTime(2026, 7, 9));
        next.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    [Fact]
    public void AddBusinessDays_NeverLandsOnWeekendOrFixedHoliday()
    {
        var start = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);

        for (var days = 1; days <= 30; days++)
        {
            var due = BusinessDayCalculator.AddBusinessDays(start, days);
            BusinessDayCalculator.IsBusinessDay(due).Should().BeTrue(
                "sonuç günü ({0}) iş günü olmalı", due.Date);
        }
    }

    [Fact]
    public void AddBusinessDays_Zero_ReturnsStart()
    {
        var start = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);

        BusinessDayCalculator.AddBusinessDays(start, 0).Should().Be(start);
    }
}
