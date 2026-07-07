using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Application.Features.CreateParty;
using HalOS.Party.Domain.Aggregates;
using HalOS.Party.Domain.Enums;
using Moq;
using Xunit;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Tests.Application;

public sealed class CreatePartyHandlerTests
{
    private readonly Mock<IPartyRepository> _parties = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private sealed class StubTenantContext : ITenantContext
    {
        public StubTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private CreatePartyHandler CreateHandler() =>
        new(_parties.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidBuyer_CreatesPartyWithTenantFromContext()
    {
        _parties.Setup(p => p.ExistsByTcknAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _parties.Setup(p => p.ExistsByVknAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreatePartyCommand(
            "Manav Ali", null, "1234567890", null, null, null,
            KeepsRecords: true, WithholdingProfile: null,
            Roles: new[] { PartyRoleType.Buyer });

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _parties.Verify(p => p.Add(It.Is<PartyAggregate>(x => x.TenantId == _tenantId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateTckn_Fails()
    {
        // Tenant içinde TCKN tekilliği ön-kontrolü (docs/02 §3.1).
        _parties.Setup(p => p.ExistsByTcknAsync("12345678901", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreatePartyCommand(
            "Mustahsil Veli", "12345678901", null, null, null, null,
            KeepsRecords: false,
            WithholdingProfile: new WithholdingProfileInput(0.0200m, 0.0100m),
            Roles: new[] { PartyRoleType.Producer });

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.TcknAlreadyInUse);
        _parties.Verify(p => p.Add(It.IsAny<PartyAggregate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateVkn_Fails()
    {
        _parties.Setup(p => p.ExistsByTcknAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _parties.Setup(p => p.ExistsByVknAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreatePartyCommand(
            "Tuccar Ltd", null, "1234567890", null, null, null,
            KeepsRecords: true, WithholdingProfile: null,
            Roles: new[] { PartyRoleType.Merchant });

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.VknAlreadyInUse);
        _parties.Verify(p => p.Add(It.IsAny<PartyAggregate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProducerWithoutProfile_PropagatesDomainError()
    {
        _parties.Setup(p => p.ExistsByTcknAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreatePartyCommand(
            "Mustahsil Profilsiz", "12345678901", null, null, null, null,
            KeepsRecords: false, WithholdingProfile: null,
            Roles: new[] { PartyRoleType.Producer });

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.ProducerRequiresWithholdingProfile);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
