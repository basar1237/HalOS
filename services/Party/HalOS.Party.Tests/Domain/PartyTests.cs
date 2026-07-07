using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Party.Domain.Aggregates;
using HalOS.Party.Domain.Enums;
using HalOS.Party.Domain.Events;
using HalOS.Party.Domain.ValueObjects;
using Xunit;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Tests.Domain;

/// <summary>
/// Party aggregate değişmezleri (docs/02 §3.1): en az bir rol; müstahsil (Producer) için stopaj
/// profili zorunlu; TCKN 11 hane / VKN 10 hane format. Tekillik değişmezi Application + DB
/// katmanında test edilir (bkz. CreatePartyHandlerTests / tenant filtresi testi).
/// </summary>
public sealed class PartyTests
{
    private static WithholdingProfile Profile() =>
        WithholdingProfile.Create(0.0200m, 0.0100m).Value;

    private static PartyAggregate NewBuyer() =>
        PartyAggregate.Register(
            Guid.NewGuid(), "Manav Ali", null, "1234567890", "Kadikoy VD", "05551112233",
            "Istanbul", keepsRecords: true, withholdingProfile: null,
            roles: new[] { PartyRoleType.Buyer }).Value;

    [Fact]
    public void Register_ValidProducer_RaisesPartyRegistered()
    {
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil Veli", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: Profile(),
            roles: new[] { PartyRoleType.Producer });

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Roles.Should().ContainSingle(r => r.Type == PartyRoleType.Producer);
        result.Value.DomainEvents.Should().ContainSingle(e => e is PartyRegistered);
    }

    [Fact]
    public void Register_NoRoles_Fails()
    {
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Adsiz", null, null, null, null, null,
            keepsRecords: false, withholdingProfile: null,
            roles: Array.Empty<PartyRoleType>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.RoleRequired);
    }

    [Fact]
    public void Register_ProducerWithoutWithholdingProfile_Fails()
    {
        // Müstahsil (Producer) değişmezi: stopaj profili tanımlı olmalı (docs/02 §3.1).
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil Profilsiz", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: null,
            roles: new[] { PartyRoleType.Producer });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.ProducerRequiresWithholdingProfile);
    }

    [Theory]
    [InlineData("123")]          // çok kısa
    [InlineData("123456789012")] // çok uzun
    [InlineData("1234567890a")]  // rakam değil
    public void Register_InvalidTckn_Fails(string tckn)
    {
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Kimlik Hatali", tckn, null, null, null, null,
            keepsRecords: true, withholdingProfile: null,
            roles: new[] { PartyRoleType.Buyer });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.InvalidTckn);
    }

    [Theory]
    [InlineData("123")]          // çok kısa
    [InlineData("12345678901")]  // çok uzun (11 hane)
    [InlineData("123456789a")]   // rakam değil
    public void Register_InvalidVkn_Fails(string vkn)
    {
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Vergi Hatali", null, vkn, null, null, null,
            keepsRecords: true, withholdingProfile: null,
            roles: new[] { PartyRoleType.Merchant });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.InvalidVkn);
    }

    [Fact]
    public void AddRole_Producer_WithoutProfile_Fails()
    {
        var buyer = NewBuyer();

        var result = buyer.AddRole(PartyRoleType.Producer);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.ProducerRequiresWithholdingProfile);
        buyer.HasRole(PartyRoleType.Producer).Should().BeFalse();
    }

    [Fact]
    public void AddRole_Duplicate_Fails()
    {
        var buyer = NewBuyer();

        var result = buyer.AddRole(PartyRoleType.Buyer);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.RoleAlreadyExists);
    }

    [Fact]
    public void AddRole_SecondRole_Succeeds_MultipleRolesAllowed()
    {
        var buyer = NewBuyer();

        var result = buyer.AddRole(PartyRoleType.Merchant);

        result.IsSuccess.Should().BeTrue();
        buyer.Roles.Should().HaveCount(2);
        buyer.HasRole(PartyRoleType.Merchant).Should().BeTrue();
    }

    [Fact]
    public void Update_RemovingProfileOnProducer_Fails()
    {
        var producer = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: Profile(),
            roles: new[] { PartyRoleType.Producer }).Value;

        var result = producer.Update(
            "Mustahsil Yeni", null, null, null, keepsRecords: false, withholdingProfile: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PartyErrors.ProducerRequiresWithholdingProfile);
    }

    [Fact]
    public void Register_Producer_RaisesProducerWithholdingProfileChanged_WithPartyRatesAndKeepsRecords()
    {
        // Müstahsil profiliyle kayıt Sales oran senkronu + Integration e-MM kararı event'ini
        // raise etmeli; oranlar VE KeepsRecords taşınmalı (docs/02 §6, §1.3 / BK-4).
        var result = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil Veli", "12345678901", null, null, null, null,
            keepsRecords: true, withholdingProfile: WithholdingProfile.Create(0.0300m, 0.0150m).Value,
            roles: new[] { PartyRoleType.Producer });

        result.IsSuccess.Should().BeTrue();
        var evt = result.Value.DomainEvents.OfType<ProducerWithholdingProfileChanged>().Should().ContainSingle().Subject;
        evt.ProducerPartyId.Should().Be(result.Value.Id);
        evt.TenantId.Should().Be(result.Value.TenantId);
        evt.AgriWithholdingRate.Should().Be(0.0300m);
        evt.FarmerSskRate.Should().Be(0.0150m);
        evt.KeepsRecords.Should().BeTrue();
    }

    [Fact]
    public void Register_NonProducer_DoesNotRaiseProfileChanged()
    {
        var buyer = NewBuyer();

        buyer.DomainEvents.OfType<ProducerWithholdingProfileChanged>().Should().BeEmpty();
    }

    [Fact]
    public void Update_ProducerProfileChanged_RaisesProfileChanged()
    {
        var producer = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: Profile(),
            roles: new[] { PartyRoleType.Producer }).Value;
        producer.ClearDomainEvents();

        var result = producer.Update(
            "Mustahsil", null, null, null, keepsRecords: false,
            withholdingProfile: WithholdingProfile.Create(0.0400m, 0.0250m).Value);

        result.IsSuccess.Should().BeTrue();
        var evt = producer.DomainEvents.OfType<ProducerWithholdingProfileChanged>().Should().ContainSingle().Subject;
        evt.AgriWithholdingRate.Should().Be(0.0400m);
        evt.FarmerSskRate.Should().Be(0.0250m);
        evt.KeepsRecords.Should().BeFalse();
    }

    [Fact]
    public void Update_ProducerProfileUnchanged_StillRaisesProfileChanged_WithKeepsRecords()
    {
        // Oranlar aynı kalsa bile müstahsil güncellemesi her seferinde event raise etmeli:
        // Integration servisinin e-MM kararı için güncel KeepsRecords bilgisine ihtiyacı var
        // (docs/02 §1.3 / BK-4). Burada KeepsRecords false → true değişimi taşınmalı.
        var producer = PartyAggregate.Register(
            Guid.NewGuid(), "Mustahsil", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: Profile(),
            roles: new[] { PartyRoleType.Producer }).Value;
        producer.ClearDomainEvents();

        var result = producer.Update(
            "Mustahsil Yeni Ad", null, null, null, keepsRecords: true,
            withholdingProfile: WithholdingProfile.Create(0.0200m, 0.0100m).Value);

        result.IsSuccess.Should().BeTrue();
        var evt = producer.DomainEvents.OfType<ProducerWithholdingProfileChanged>().Should().ContainSingle().Subject;
        evt.AgriWithholdingRate.Should().Be(0.0200m);
        evt.FarmerSskRate.Should().Be(0.0100m);
        evt.KeepsRecords.Should().BeTrue();
    }

    [Fact]
    public void AddRole_Producer_WithProfile_RaisesProfileChanged()
    {
        // Önce profili olan bir Buyer oluştur, sonra Producer rolü ekle → senkron event'i raise et.
        var party = PartyAggregate.Register(
            Guid.NewGuid(), "Coklu Rol", "12345678901", null, null, null, null,
            keepsRecords: false, withholdingProfile: Profile(),
            roles: new[] { PartyRoleType.Buyer }).Value;
        party.ClearDomainEvents();

        var result = party.AddRole(PartyRoleType.Producer);

        result.IsSuccess.Should().BeTrue();
        party.DomainEvents.OfType<ProducerWithholdingProfileChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Deactivate_RaisesEvent_AndIsIdempotentFailure()
    {
        var buyer = NewBuyer();

        var first = buyer.Deactivate();
        first.IsSuccess.Should().BeTrue();
        buyer.IsActive.Should().BeFalse();
        buyer.DomainEvents.Should().Contain(e => e is PartyDeactivated);

        var second = buyer.Deactivate();
        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(PartyErrors.AlreadyInactive);
    }
}
