using FluentAssertions;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Features.Users.Login;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Enums;
using HalOS.Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace HalOS.Identity.Tests.Application;

public sealed class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<ITotpService> _totp = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginHandler CreateHandler() =>
        new(_users.Object, _hasher.Object, _tokens.Object, _totp.Object, _unitOfWork.Object);

    private static User BuildUser(bool twoFactor = false)
    {
        var user = User.Register(
            Guid.NewGuid(),
            Email.Create("ali@hal.com").Value,
            PasswordHash.Create("stored-hash").Value,
            "Ali Veli",
            SystemRole.Owner).Value;

        if (twoFactor)
        {
            user.BeginTwoFactorSetup("SECRET");
            user.EnableTwoFactor();
        }

        return user;
    }

    private void SetupTokens()
    {
        _tokens.Setup(t => t.CreateTokenPair(It.IsAny<User>()))
            .Returns(new TokenPair("access", DateTime.UtcNow.AddMinutes(15),
                "refresh", DateTime.UtcNow.AddDays(7)));
        _tokens.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns("refresh-hash");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        var user = BuildUser();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", "correct")).Returns(true);
        SetupTokens();

        var result = await CreateHandler().Handle(
            new LoginCommand("ali@hal.com", "correct", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access");
        result.Value.TenantId.Should().Be(user.TenantId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = BuildUser();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await CreateHandler().Handle(
            new LoginCommand("ali@hal.com", "wrong", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsInvalidCredentials()
    {
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new LoginCommand("nobody@hal.com", "x", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_TwoFactorEnabled_MissingCode_RequiresCode()
    {
        var user = BuildUser(twoFactor: true);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("ali@hal.com", "correct", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorRequired);
    }

    [Fact]
    public async Task Handle_TwoFactorEnabled_ValidCode_Succeeds()
    {
        var user = BuildUser(twoFactor: true);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _totp.Setup(t => t.VerifyCode(It.IsAny<string>(), "123456")).Returns(true);
        SetupTokens();

        var result = await CreateHandler().Handle(
            new LoginCommand("ali@hal.com", "correct", "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
