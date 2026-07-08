using FluentAssertions;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using Xunit;

namespace HalOS.Integration.Tests.Domain;

/// <summary>
/// ProductPassport (künye) aggregate testleri (docs/02 §3.5, docs/03 BK-4). Saf, in-memory. Künye
/// ürün/kalem bazlı; HKS 19-haneli kod Issued'da atanır (QR ile sorgulanır).
/// </summary>
public sealed class ProductPassportTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime Received = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private static ProductPassport Sample(decimal quantity = 100m) =>
        ProductPassport.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            quantity, "Kilogram", Received).Value;

    [Fact]
    public void Create_StartsAsDraft_WithoutCode()
    {
        var passport = Sample(quantity: 250.500m);

        passport.Status.Should().Be(ProductPassportStatus.Draft);
        passport.PassportCode.Should().BeNull();
        passport.Quantity.Should().Be(250.500m);
        passport.UnitCode.Should().Be("Kilogram");
    }

    [Fact]
    public void Create_NonPositiveQuantity_Fails()
    {
        ProductPassport.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                0m, "Kilogram", Received)
            .Error.Should().Be(ProductPassport.ProductPassportErrors.NonPositiveQuantity);
    }

    [Fact]
    public void MarkIssued_AssignsCodeAndStatus_AndRaisesEvent()
    {
        var passport = Sample();

        var result = passport.MarkIssued("1234567890123456789");

        result.IsSuccess.Should().BeTrue();
        passport.Status.Should().Be(ProductPassportStatus.Issued);
        passport.PassportCode.Should().Be("1234567890123456789");

        var evt = passport.DomainEvents.OfType<ProductPassportIssued>().Should().ContainSingle().Subject;
        evt.ConsignmentItemId.Should().Be(passport.ConsignmentItemId);
        evt.PassportCode.Should().Be("1234567890123456789");
        evt.TenantId.Should().Be(Tenant);
    }

    [Fact]
    public void MarkIssued_EmptyCode_Fails()
    {
        Sample().MarkIssued("  ").Error.Should().Be(ProductPassport.ProductPassportErrors.PassportCodeRequired);
    }
}
