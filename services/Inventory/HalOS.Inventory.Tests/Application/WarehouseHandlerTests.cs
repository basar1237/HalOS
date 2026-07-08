using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Features.CreateWarehouse;
using HalOS.Inventory.Application.Features.ListWarehouses;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Inventory.Tests.Application;

/// <summary>
/// Warehouse feature testleri (docs/06 S2.1 depo lokasyonu). Gerçek InventoryDbContext (InMemory) +
/// gerçek WarehouseRepository ile: depo oluşturma, kod tekilliği (tenant içinde) ve listeleme.
/// </summary>
public sealed class WarehouseHandlerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static InventoryDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new InventoryDbContext(options, tenantContext);
    }

    [Fact]
    public async Task CreateWarehouse_PersistsWarehouse_WithTenantFromContext()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new CreateWarehouseHandler(new WarehouseRepository(ctx), stub, ctx);
            var result = await handler.Handle(
                new CreateWarehouseCommand("Soğuk Hava Deposu", "SOGUK", IsDefault: false),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var wh = await ctx.Warehouses.SingleAsync();
            wh.Name.Should().Be("Soğuk Hava Deposu");
            wh.Code.Should().Be("SOGUK");
            wh.IsDefault.Should().BeFalse();
            wh.TenantId.Should().Be(tenantId);
        }
    }

    [Fact]
    public async Task CreateWarehouse_DuplicateCode_Fails_WithinTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            ctx.Warehouses.Add(Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new CreateWarehouseHandler(new WarehouseRepository(ctx), stub, ctx);
            var result = await handler.Handle(
                new CreateWarehouseCommand("İkinci", "MERKEZ", IsDefault: false),
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(WarehouseErrors.CodeAlreadyExists);
        }
    }

    [Fact]
    public async Task CreateWarehouse_NewDefault_DemotesExistingDefault_SingleDefaultRemains()
    {
        // Değişmez: tenant başına tek varsayılan depo (docs/06 S2.1). Önce consumer MERKEZ'i (default)
        // oluşturmuş gibi seed edilir; ardından manuel IsDefault=true ikinci depo yaratılır → MERKEZ
        // düşürülür, tek varsayılan (yeni depo) kalır ve GetDefaultAsync deterministik onu döner.
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.Warehouses.Add(Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value);
            await seed.SaveChangesAsync();
        }

        Guid newDefaultId;
        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new CreateWarehouseHandler(new WarehouseRepository(ctx), stub, ctx);
            var result = await handler.Handle(
                new CreateWarehouseCommand("Soğuk Hava Deposu", "SOGUK", IsDefault: true),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            newDefaultId = result.Value;
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var defaults = await ctx.Warehouses.Where(w => w.IsDefault).ToListAsync();
            defaults.Should().ContainSingle(); // yalnız tek varsayılan kaldı
            defaults[0].Id.Should().Be(newDefaultId);
            defaults[0].Code.Should().Be("SOGUK");

            // Eski MERKEZ düşürülmüş olmalı.
            var merkez = await ctx.Warehouses.SingleAsync(w => w.Code == "MERKEZ");
            merkez.IsDefault.Should().BeFalse();

            // GetDefaultAsync deterministik olarak tek varsayılanı (yeni depo) döner.
            var def = await new WarehouseRepository(ctx).GetDefaultAsync();
            def!.Id.Should().Be(newDefaultId);
        }
    }

    [Fact]
    public async Task CreateWarehouse_FirstDefault_BecomesDefault_WhenNonePresent()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new CreateWarehouseHandler(new WarehouseRepository(ctx), stub, ctx);
            var result = await handler.Handle(
                new CreateWarehouseCommand("Merkez Depo", "MERKEZ", IsDefault: true),
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var defaults = await ctx.Warehouses.Where(w => w.IsDefault).ToListAsync();
            defaults.Should().ContainSingle();
            defaults[0].Code.Should().Be("MERKEZ");
        }
    }

    [Fact]
    public async Task ListWarehouses_ReturnsOnlyCurrentTenants_OrderedByName()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.Warehouses.Add(Warehouse.Create(tenantA, "Zeta Depo", "ZETA", isDefault: false).Value);
            seed.Warehouses.Add(Warehouse.Create(tenantA, "Alfa Depo", "ALFA", isDefault: true).Value);
            seed.Warehouses.Add(Warehouse.Create(tenantB, "Başka Tenant", "BASKA", isDefault: true).Value);
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stubA, dbName))
        {
            var handler = new ListWarehousesHandler(new WarehouseRepository(ctx));
            var result = await handler.Handle(new ListWarehousesQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2); // yalnız tenantA
            result.Value.Select(w => w.Name).Should().ContainInOrder("Alfa Depo", "Zeta Depo");
        }
    }
}
