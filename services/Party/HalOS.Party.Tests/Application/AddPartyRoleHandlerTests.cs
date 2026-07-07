using FluentAssertions;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Application.Features.AddPartyRole;
using HalOS.Party.Domain.Aggregates;
using HalOS.Party.Domain.Enums;
using HalOS.Party.Domain.ValueObjects;
using Moq;
using Xunit;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Tests.Application;

public sealed class AddPartyRoleHandlerTests
{
    private readonly Mock<IPartyRepository> _parties = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AddPartyRoleHandler CreateHandler() => new(_parties.Object, _unitOfWork.Object);

    private static PartyAggregate NewBuyer() =>
        PartyAggregate.Register(
            Guid.NewGuid(), "Manav Ali", null, null, null, null, null,
            keepsRecords: true, withholdingProfile: null,
            roles: new[] { PartyRoleType.Buyer }).Value;

    [Fact]
    public async Task Handle_PartyNotFound_Fails()
    {
        _parties.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyAggregate?)null);

        var result = await CreateHandler().Handle(
            new AddPartyRoleCommand(Guid.NewGuid(), PartyRoleType.Merchant), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AddSecondRole_Succeeds()
    {
        var buyer = NewBuyer();
        _parties.Setup(p => p.GetByIdAsync(buyer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyer);

        var result = await CreateHandler().Handle(
            new AddPartyRoleCommand(buyer.Id, PartyRoleType.Consignor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        buyer.HasRole(PartyRoleType.Consignor).Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddProducerWithoutProfile_Fails()
    {
        var buyer = NewBuyer();
        _parties.Setup(p => p.GetByIdAsync(buyer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyer);

        var result = await CreateHandler().Handle(
            new AddPartyRoleCommand(buyer.Id, PartyRoleType.Producer), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.ProducerRequiresWithholdingProfile);
    }
}
