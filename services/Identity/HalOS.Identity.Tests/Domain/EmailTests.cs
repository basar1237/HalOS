using FluentAssertions;
using HalOS.Identity.Domain.ValueObjects;
using Xunit;

namespace HalOS.Identity.Tests.Domain;

public sealed class EmailTests
{
    [Theory]
    [InlineData("ali@hal.com")]
    [InlineData("Mehmet.Yildiz@example.co")]
    public void Create_ValidEmail_Succeeds(string input)
    {
        var result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(input.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Empty_Fails(string? input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@nolocal.com")]
    public void Create_InvalidFormat_Fails(string input)
    {
        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Equality_IsStructural_AndCaseInsensitive()
    {
        var a = Email.Create("ALI@hal.com").Value;
        var b = Email.Create("ali@HAL.com").Value;

        a.Should().Be(b);
    }
}
