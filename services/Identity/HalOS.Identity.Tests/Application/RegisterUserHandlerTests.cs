using FluentAssertions;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Application.Features.Users.RegisterUser;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Enums;
using HalOS.Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace HalOS.Identity.Tests.Application;

public sealed class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterUserHandler CreateHandler() =>
        new(_users.Object, _tenants.Object, _hasher.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidCommand_RegistersUser()
    {
        var tenantId = Guid.NewGuid();
        _tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant.Create("Hal").Value);
        // Tekillik kontrolü tam olarak kayıt tenant'ı + e-posta ile yapılmalı (maskesiz yol).
        _users.Setup(u => u.ExistsByEmailInTenantAsync(
                tenantId, It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-pw");

        var command = new RegisterUserCommand(
            tenantId, "yeni@hal.com", "parola12", "Yeni Kullanici", SystemRole.Cashier);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _users.Verify(u => u.ExistsByEmailInTenantAsync(
            tenantId,
            It.Is<Email>(e => e.Value == "yeni@hal.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.Add(It.Is<User>(x => x.TenantId == tenantId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TenantMissing_Fails()
    {
        var tenantId = Guid.NewGuid();
        _tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var command = new RegisterUserCommand(
            tenantId, "yeni@hal.com", "parola12", "Ad", SystemRole.Cashier);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_Fails()
    {
        var tenantId = Guid.NewGuid();
        _tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant.Create("Hal").Value);
        // Kayıt tenant'ı kapsamında e-posta zaten mevcut → dostça Result hatası dönmeli.
        _users.Setup(u => u.ExistsByEmailInTenantAsync(
                tenantId, It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterUserCommand(
            tenantId, "var@hal.com", "parola12", "Ad", SystemRole.Cashier);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailAlreadyInUse);
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
