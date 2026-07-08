using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using Xunit;

namespace HalOS.Sales.Tests.Domain;

/// <summary>Consignment (Mal Geliş) aggregate testleri (docs/02 §3.2, docs/03 M3).</summary>
public sealed class ConsignmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _producerId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static readonly DateTime ReceivedAt = new(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Receive_ValidItems_Succeeds_AndRaisesEvent()
    {
        var items = new[] { new Consignment.ItemInput(_productId, 50m, UnitOfMeasure.Crate) };

        var result = Consignment.Receive(_tenantId, _producerId, ReceivedAt, "IRS-123", _userId, items);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ConsignmentStatus.Received);
        result.Value.Items.Should().ContainSingle();
        result.Value.DomainEvents.OfType<ConsignmentReceived>().Should().ContainSingle();
    }

    [Fact]
    public void Receive_ValidItems_CarriesItemsInEvent()
    {
        var items = new[] { new Consignment.ItemInput(_productId, 50m, UnitOfMeasure.Crate) };

        var result = Consignment.Receive(_tenantId, _producerId, ReceivedAt, "IRS-123", _userId, items);

        var evt = result.Value.DomainEvents.OfType<ConsignmentReceived>().Single();
        var expectedItem = result.Value.Items.Single();

        evt.TenantId.Should().Be(_tenantId);
        evt.ConsignmentId.Should().Be(result.Value.Id);
        evt.ProducerPartyId.Should().Be(_producerId);
        evt.Items.Should().ContainSingle();

        var eventItem = evt.Items.Single();
        eventItem.ConsignmentItemId.Should().Be(expectedItem.Id);
        eventItem.ProductId.Should().Be(_productId);
        eventItem.Quantity.Should().Be(50m);
        eventItem.UnitCode.Should().Be(UnitOfMeasure.Crate.ToString());
    }

    [Fact]
    public void Receive_NoItems_Fails()
    {
        var result = Consignment.Receive(
            _tenantId, _producerId, ReceivedAt, null, _userId, Array.Empty<Consignment.ItemInput>());

        result.Error.Should().Be(ConsignmentErrors.ItemRequired);
    }

    [Fact]
    public void Receive_ZeroQuantityItem_Fails()
    {
        var items = new[] { new Consignment.ItemInput(_productId, 0m, UnitOfMeasure.Kilogram) };

        var result = Consignment.Receive(_tenantId, _producerId, ReceivedAt, null, _userId, items);

        result.Error.Should().Be(ConsignmentErrors.InvalidQuantity);
    }

    [Fact]
    public void Receive_MissingProducer_Fails()
    {
        var items = new[] { new Consignment.ItemInput(_productId, 10m, UnitOfMeasure.Kilogram) };

        var result = Consignment.Receive(_tenantId, Guid.Empty, ReceivedAt, null, _userId, items);

        result.Error.Should().Be(ConsignmentErrors.ProducerRequired);
    }
}
