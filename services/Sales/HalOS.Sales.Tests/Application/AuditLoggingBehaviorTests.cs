using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.BuildingBlocks.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// <see cref="AuditLoggingBehavior{TRequest,TResponse}"/>'ın davranışını gerçek bir EF Core
/// (InMemory) DbContext + gerçek <see cref="AuditLogSink{TContext}"/> ile doğrular (docs/05 §3.11):
/// KOMUT çalıştığında audit_log'a kim/ne/ne zaman yazılır; QUERY çalıştığında YAZILMAZ (docs/07 §5).
/// </summary>
public sealed class AuditLoggingBehaviorTests
{
    /// <summary>Test için tenant'lı DbContext; audit_log eşlemesini base'ten (ConfigureAuditLog) alır.</summary>
    private sealed class TestDbContext : TenantDbContextBase
    {
        public TestDbContext(DbContextOptions options, ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }
    }

    private sealed class StubAuditActor : IAuditActor
    {
        public StubAuditActor(Guid userId)
        {
            UserId = userId;
            HasUser = userId != Guid.Empty;
        }

        public Guid UserId { get; }
        public bool HasUser { get; }
    }

    // Denetlenecek örnek komut ve denetlenmeyecek örnek query (marker arayüzlerle).
    private sealed record SampleCommand : ICommand;

    private sealed record SampleQuery : IQuery<int>;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private TestDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"audit-{Guid.NewGuid()}")
                .Options,
            new StubTenantContext(_tenantId));

    [Fact]
    public async Task Handle_Command_WritesAuditLog_WithWhoWhatWhen()
    {
        await using var context = CreateContext();
        var behavior = new AuditLoggingBehavior<SampleCommand, Result>(
            new AuditLogSink<TestDbContext>(context),
            new StubAuditActor(_userId),
            new StubTenantContext(_tenantId),
            NullLogger<AuditLoggingBehavior<SampleCommand, Result>>.Instance);

        var before = DateTime.UtcNow;
        var result = await behavior.Handle(
            new SampleCommand(),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var log = await context.AuditLogs.SingleAsync();
        log.Action.Should().Be(nameof(SampleCommand)); // ne
        log.UserId.Should().Be(_userId);               // kim
        log.TenantId.Should().Be(_tenantId);           // tenant (BK-8)
        log.CreatedOnUtc.Should().BeOnOrAfter(before);  // ne zaman
    }

    [Fact]
    public async Task Handle_Query_DoesNotWriteAuditLog()
    {
        await using var context = CreateContext();
        var behavior = new AuditLoggingBehavior<SampleQuery, Result<int>>(
            new AuditLogSink<TestDbContext>(context),
            new StubAuditActor(_userId),
            new StubTenantContext(_tenantId),
            NullLogger<AuditLoggingBehavior<SampleQuery, Result<int>>>.Instance);

        var result = await behavior.Handle(
            new SampleQuery(),
            () => Task.FromResult(Result.Success(42)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);

        (await context.AuditLogs.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AnonymousCommand_WritesAuditLog_WithNullUser()
    {
        await using var context = CreateContext();
        var behavior = new AuditLoggingBehavior<SampleCommand, Result>(
            new AuditLogSink<TestDbContext>(context),
            new StubAuditActor(Guid.Empty), // HasUser == false
            new StubTenantContext(_tenantId),
            NullLogger<AuditLoggingBehavior<SampleCommand, Result>>.Instance);

        await behavior.Handle(
            new SampleCommand(),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        var log = await context.AuditLogs.SingleAsync();
        log.UserId.Should().BeNull();
        log.Action.Should().Be(nameof(SampleCommand));
    }

    /// <summary>
    /// Best-effort denetim (M2): ikinci (audit) save patlarsa istisna YUTULUR ve komut sonucu
    /// ETKİLENMEZ — çünkü komut zaten uygulanmıştır. Denetim-yazım hatası committed komutu
    /// maskelemez (docs/07 §10; sınıf XML doc'u best-effort kuralı).
    /// </summary>
    [Fact]
    public async Task Handle_AuditSaveThrows_DoesNotMaskCommandResult()
    {
        var behavior = new AuditLoggingBehavior<SampleCommand, Result>(
            new ThrowingAuditLogSink(),
            new StubAuditActor(_userId),
            new StubTenantContext(_tenantId),
            NullLogger<AuditLoggingBehavior<SampleCommand, Result>>.Instance);

        var act = async () => await behavior.Handle(
            new SampleCommand(),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Denetim save'i patlasa da Handle istisna atmaz ve komutun başarı sonucunu döndürür.
        var result = await act.Should().NotThrowAsync();
        result.Subject.IsSuccess.Should().BeTrue();
    }

    /// <summary>Denetim save'inde daima patlayan sink; best-effort yutmayı test eder.</summary>
    private sealed class ThrowingAuditLogSink : IAuditLogSink
    {
        public void Add(AuditEntry entry)
        {
        }

        public Task SaveAsync(CancellationToken ct) =>
            throw new InvalidOperationException("Denetim save'i başarısız (test).");
    }
}
