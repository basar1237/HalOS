using FluentAssertions;
using HalOS.Party.Domain.ValueObjects;
using Xunit;

namespace HalOS.Party.Tests.Domain;

/// <summary>
/// Stopaj profili value object doğrulaması (docs/02 §1.3, §3.1). Oranlar decimal (asla float —
/// BK-2); 0 ile 1 arası olmalı. Yapısal eşitlik value object semantiğidir.
/// </summary>
public sealed class WithholdingProfileTests
{
    [Fact]
    public void Create_ValidRates_Succeeds()
    {
        var result = WithholdingProfile.Create(0.0200m, 0.0100m);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgriWithholdingRate.Should().Be(0.0200m);
        result.Value.FarmerSskRate.Should().Be(0.0100m);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_AgriRateOutOfRange_Fails(decimal rate)
    {
        var result = WithholdingProfile.Create(rate, 0.0100m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WithholdingProfileErrors.AgriRateOutOfRange);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_SskRateOutOfRange_Fails(decimal rate)
    {
        var result = WithholdingProfile.Create(0.0200m, rate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WithholdingProfileErrors.SskRateOutOfRange);
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = WithholdingProfile.Create(0.0200m, 0.0100m).Value;
        var b = WithholdingProfile.Create(0.0200m, 0.0100m).Value;

        a.Should().Be(b);
    }
}
