using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Features.Reports.CurrentAccountAgingReport;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Infrastructure.Persistence;
using HalOS.Finance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Finance.Tests.Application;

/// <summary>
/// Cari yaşlandırma raporu handler'ının GERÇEK InMemory DbContext + GERÇEK
/// <see cref="CurrentAccountRepository"/> ile entegrasyon testleri (docs/03 M10, docs/07 §7).
/// Yalnız vade taşıyan müstahsil hakediş (Settlement) hareketleri yaşlandırılır; tenant global
/// filter otomatik (BK-8); tutarlar decimal (BK-2). Yeni tablo/migration yok — mevcut veri okunur.
///
/// Referans tarih (AsOf) = 2026-07-31. Kovalar:
/// - Güncel: DueDate &gt;= 07-31.
/// - 0-15 gün gecikmiş: 07-16 &lt;= DueDate &lt; 07-31.
/// - 16-30 gün gecikmiş: 07-01 &lt;= DueDate &lt; 07-16.
/// - 31+ gün gecikmiş: DueDate &lt; 07-01.
/// </summary>
public sealed class CurrentAccountAgingReportHandlerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static FinanceDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new FinanceDbContext(options, tenantContext);
    }

    private static readonly DateTime AsOf = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Occurred = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>Vade tarihli net hakediş (Settlement/Credit) taşıyan bir cari üretir.</summary>
    private static CurrentAccount AccountWithSettlement(Guid tenantId, decimal net, DateTime dueDate)
    {
        var account = CurrentAccount.Open(tenantId, Guid.NewGuid()).Value;
        account.RecordSettlementCredit(Guid.NewGuid(), net, dueDate, Occurred);
        return account;
    }

    [Fact]
    public async Task Aging_BucketsSettlementsByDueDateAge()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            // Güncel: vade referanstan sonra.
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 100m, new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc)));
            // 0-15 gün gecikmiş: 07-20 (11 gün gecikme).
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 200m, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)));
            // 16-30 gün gecikmiş: 07-10 (21 gün gecikme).
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 300m, new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc)));
            // 31+ gün gecikmiş: 06-20 (41 gün gecikme).
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 400m, new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc)));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.Current.Amount.Should().Be(100m);
        dto.Current.AccountCount.Should().Be(1);
        dto.Days0To15.Amount.Should().Be(200m);
        dto.Days0To15.AccountCount.Should().Be(1);
        dto.Days16To30.Amount.Should().Be(300m);
        dto.Days16To30.AccountCount.Should().Be(1);
        dto.Days31Plus.Amount.Should().Be(400m);
        dto.Days31Plus.AccountCount.Should().Be(1);
        dto.TotalAmount.Should().Be(1000m);
        dto.TotalAccountCount.Should().Be(4);
    }

    [Fact]
    public async Task Aging_DueDateExactlyOnAsOf_IsCurrent()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            // Vade tam referans günü → henüz gecikmemiş (güncel).
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 150m, AsOf));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.Value.Current.Amount.Should().Be(150m);
        result.Value.Days0To15.Amount.Should().Be(0m);
        result.Value.TotalAmount.Should().Be(150m);
    }

    [Fact]
    public async Task Aging_MultipleSettlementsSameAccount_CountsAccountOncePerBucket()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid();

        await using (var seed = CreateContext(stub, dbName))
        {
            // Tek cari, iki hakediş: ikisi de 0-15 kovasına düşer → tutarlar toplanır, cari 1 sayılır.
            var account = CurrentAccount.Open(tenantId, partyId).Value;
            account.RecordSettlementCredit(Guid.NewGuid(), 200m, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc), Occurred);
            account.RecordSettlementCredit(Guid.NewGuid(), 50m, new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc), Occurred);
            seed.CurrentAccounts.Add(account);
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.Value.Days0To15.Amount.Should().Be(250m);
        result.Value.Days0To15.AccountCount.Should().Be(1);
        result.Value.TotalAccountCount.Should().Be(1);
    }

    [Fact]
    public async Task Aging_IgnoresEntriesWithoutDueDate()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            // Vadeli hakediş (0-15 kovası).
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantId, 200m, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)));
            // Alıcı satış borcu: DueDate null → yaşlandırmaya girmemeli.
            var buyer = CurrentAccount.Open(tenantId, Guid.NewGuid()).Value;
            buyer.RecordSaleDebit(Guid.NewGuid(), 999m, Occurred);
            seed.CurrentAccounts.Add(buyer);
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.Value.TotalAmount.Should().Be(200m);
        result.Value.TotalAccountCount.Should().Be(1); // yalnız müstahsil carisi
    }

    [Fact]
    public async Task Aging_RespectsTenantFilter_OtherTenantExcluded()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantA, 100m, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)));
            seed.CurrentAccounts.Add(AccountWithSettlement(tenantB, 500m, new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stubA, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.Value.Days0To15.Amount.Should().Be(100m); // yalnız tenant A
        result.Value.TotalAmount.Should().Be(100m);
        result.Value.TotalAccountCount.Should().Be(1);
    }

    [Fact]
    public async Task Aging_NoSettlements_ReturnsEmptyBuckets()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CurrentAccountAgingReportHandler(new CurrentAccountRepository(ctx));

        var result = await handler.Handle(new CurrentAccountAgingReportQuery(AsOf), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAmount.Should().Be(0m);
        result.Value.TotalAccountCount.Should().Be(0);
        result.Value.Current.AccountCount.Should().Be(0);
    }

    [Fact]
    public void Validator_RejectsDefaultAsOf()
    {
        var validator = new CurrentAccountAgingReportValidator();
        var result = validator.Validate(new CurrentAccountAgingReportQuery(default));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_AllowsConcreteAsOf()
    {
        var validator = new CurrentAccountAgingReportValidator();
        var result = validator.Validate(new CurrentAccountAgingReportQuery(AsOf));
        result.IsValid.Should().BeTrue();
    }
}
