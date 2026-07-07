using FluentAssertions;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Features.RecordPayment;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;
using Moq;
using Xunit;

namespace HalOS.Finance.Tests.Application;

/// <summary>
/// RecordPaymentHandler unit testleri (docs/03 M6). Mevcut cariye ödeme işler; cari yoksa açar.
/// BK-6 nakit eşiği domain'de doğrulanır (handler hatayı propagasyon eder, SaveChanges çağrılmaz).
/// Mock repo/port (docs/07 §7).
/// </summary>
public sealed class RecordPaymentHandlerTests
{
    private readonly Mock<ICurrentAccountRepository> _accounts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _partyId = Guid.NewGuid();

    private RecordPaymentHandler CreateHandler() =>
        new(_accounts.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    [Fact]
    public async Task Handle_ExistingAccount_RecordsPayment_Saves()
    {
        var account = CurrentAccount.Open(_tenantId, _partyId).Value;
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CreateHandler().Handle(
            new RecordPaymentCommand(_partyId, 500.00m, PaymentChannel.Bank, "TR-REF", DateTime.UtcNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Entries.Should().ContainSingle(e => e.Type == EntryType.Payment);
        _accounts.Verify(r => r.Update(account), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoAccount_OpensAccount_RecordsPayment()
    {
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrentAccount?)null);

        var result = await CreateHandler().Handle(
            new RecordPaymentCommand(_partyId, 100.00m, PaymentChannel.Cash, null, DateTime.UtcNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _accounts.Verify(r => r.Add(It.Is<CurrentAccount>(a => a.PartyId == _partyId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CashAboveLimit_Fails_DoesNotSave_BK6()
    {
        var account = CurrentAccount.Open(_tenantId, _partyId).Value;
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CreateHandler().Handle(
            new RecordPaymentCommand(_partyId, 7500.00m, PaymentChannel.Cash, null, DateTime.UtcNow),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CurrentAccountErrors.CashLimitExceeded);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
