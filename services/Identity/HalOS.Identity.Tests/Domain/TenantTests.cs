using FluentAssertions;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Events;
using Xunit;

namespace HalOS.Identity.Tests.Domain;

public sealed class TenantTests
{
    [Fact]
    public void Create_Valid_RaisesTenantCreated()
    {
        var result = Tenant.Create("Ege Hal Komisyon");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(e => e is TenantCreated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyName_Fails(string? name)
    {
        var result = Tenant.Create(name);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.NameRequired);
    }
}
