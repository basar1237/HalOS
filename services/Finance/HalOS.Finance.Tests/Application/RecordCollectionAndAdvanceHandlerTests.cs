using FluentAssertions;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Features.RecordAdvance;
using HalOS.Finance.Application.Features.RecordCollection;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;
using Moq;
using Xunit;

namespace HalOS.Finance.Tests.Application;

/// <summary>
/// RecordCollection / RecordAdvance handler unit testleri (docs/03 M6). Tahsilat alacak, avans
/// borç hareketidir; BK-6 nakit eşiği domain'de doğrulanır. Mock repo/port (docs/07 §7).
/// </summary>
public sealed class RecordCollectionAndAdvanceHandlerTests
{
    private readonly Mock<ICurrentAccountRepository> _accounts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _partyId = Guid.NewGuid();

    private RecordCollectionHandler CollectionHandler() =>
        new(_accounts.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    private RecordAdvanceHandler AdvanceHandler() =>
        new(_accounts.Object, new StubTenantContext(_tenantId), _unitOfWork.Object);

    [Fact]
    public async Task Collection_ExistingAccount_RecordsCreditEntry_Saves()
    {
        var account = CurrentAccount.Open(_tenantId, _partyId).Value;
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CollectionHandler().Handle(
            new RecordCollectionCommand(_partyId, 250.00m, PaymentChannel.Bank, "TR-REF", DateTime.UtcNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Entries.Should().ContainSingle(e => e.Type == EntryType.Collection
            && e.Direction == EntryDirection.Credit);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Collection_CashAboveLimit_Fails_BK6()
    {
        var account = CurrentAccount.Open(_tenantId, _partyId).Value;
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await CollectionHandler().Handle(
            new RecordCollectionCommand(_partyId, 8000.00m, PaymentChannel.Cash, null, DateTime.UtcNow),
            CancellationToken.None);

        result.Error.Should().Be(CurrentAccountErrors.CashLimitExceeded);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Advance_ExistingAccount_RecordsDebitEntry_Saves()
    {
        var account = CurrentAccount.Open(_tenantId, _partyId).Value;
        _accounts.Setup(r => r.GetByPartyIdAsync(_partyId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await AdvanceHandler().Handle(
            new RecordAdvanceCommand(_partyId, 300.00m, PaymentChannel.Bank, null, DateTime.UtcNow),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Entries.Should().ContainSingle(e => e.Type == EntryType.Advance
            && e.Direction == EntryDirection.Debit);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
