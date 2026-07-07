using FluentAssertions;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Enums;
using HalOS.Identity.Domain.Events;
using HalOS.Identity.Domain.ValueObjects;
using Xunit;

namespace HalOS.Identity.Tests.Domain;

public sealed class UserTests
{
    private static User NewUser(Guid? tenantId = null)
    {
        var email = Email.Create("kasiyer@hal.com").Value;
        var hash = PasswordHash.Create("hashed").Value;
        return User.Register(
            tenantId ?? Guid.NewGuid(),
            email,
            hash,
            "Kasiyer Bir",
            SystemRole.Cashier).Value;
    }

    [Fact]
    public void Register_RaisesUserRegisteredEvent()
    {
        var tenantId = Guid.NewGuid();
        var user = NewUser(tenantId);

        user.TenantId.Should().Be(tenantId);
        user.IsActive.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle(e => e is UserRegistered);
    }

    [Fact]
    public void Register_EmptyFullName_Fails()
    {
        var email = Email.Create("x@hal.com").Value;
        var hash = PasswordHash.Create("h").Value;

        var result = User.Register(Guid.NewGuid(), email, hash, "  ", SystemRole.Owner);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.FullNameRequired);
    }

    [Fact]
    public void EnableTwoFactor_WithoutSetup_Fails()
    {
        var user = NewUser();

        var result = user.EnableTwoFactor();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorNotSetUp);
        user.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public void BeginTwoFactorSetup_ThenEnable_Succeeds_AndRaisesEvent()
    {
        var user = NewUser();

        user.BeginTwoFactorSetup("SECRET32").IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse("kod doğrulanmadan etkinleşmemeli");

        var enable = user.EnableTwoFactor();

        enable.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.DomainEvents.Should().Contain(e => e is UserTwoFactorEnabled);
    }

    [Fact]
    public void RefreshToken_IssueThenRevoke_Rotates()
    {
        var user = NewUser();

        var token = user.IssueRefreshToken("hash-1", DateTime.UtcNow.AddDays(7));
        token.IsActive.Should().BeTrue();

        var revoke = user.RevokeRefreshToken("hash-1");
        revoke.IsSuccess.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RevokeRefreshToken_Unknown_Fails()
    {
        var user = NewUser();

        var revoke = user.RevokeRefreshToken("does-not-exist");

        revoke.IsFailure.Should().BeTrue();
        revoke.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }
}
