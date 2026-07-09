using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Features.CreateProduct;
using HalOS.Inventory.Application.Features.DeactivateProduct;
using HalOS.Inventory.Application.Features.GetProduct;
using HalOS.Inventory.Application.Features.ListProducts;
using HalOS.Inventory.Application.Features.UpdateProduct;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Inventory.Tests.Application;

/// <summary>
/// Product kataloğu feature testleri (docs/03 M2). Gerçek InventoryDbContext (InMemory) + gerçek
/// ProductRepository ile: oluşturma, tenant izolasyonu, güncelleme, pasifleştirme (onlyActive filtre).
/// WarehouseHandlerTests deseniyle birebir.
/// </summary>
public sealed class ProductHandlerTests
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
    public async Task CreateProduct_Persists_WithTenantAndDefaults()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        Guid id;
        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new CreateProductHandler(new ProductRepository(ctx), stub, ctx);
            var result = await handler.Handle(
                new CreateProductCommand("Domates", "Sebze", UnitOfMeasure.Crate),
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            id = result.Value;
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var product = await ctx.Products.SingleAsync();
            product.Id.Should().Be(id);
            product.Name.Should().Be("Domates");
            product.Category.Should().Be("Sebze");
            product.DefaultUnit.Should().Be(UnitOfMeasure.Crate);
            product.IsActive.Should().BeTrue();
            product.TenantId.Should().Be(tenantId);
        }
    }

    [Fact]
    public async Task CreateProduct_EmptyName_Fails()
    {
        var dbName = Guid.NewGuid().ToString();
        var stub = new StubTenantContext { TenantId = Guid.NewGuid() };

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CreateProductHandler(new ProductRepository(ctx), stub, ctx);
        var result = await handler.Handle(
            new CreateProductCommand("  ", null, UnitOfMeasure.Kilogram),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameRequired);
    }

    [Fact]
    public async Task UpdateProduct_ChangesFields()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        Guid id;
        await using (var ctx = CreateContext(stub, dbName))
        {
            var product = Product.Create(tenantId, "Domates", "Sebze", UnitOfMeasure.Crate).Value;
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            id = product.Id;
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new UpdateProductHandler(new ProductRepository(ctx), ctx);
            var result = await handler.Handle(
                new UpdateProductCommand(id, "Salkım Domates", "Sebze/Meyve", UnitOfMeasure.Kilogram),
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var product = await ctx.Products.SingleAsync();
            product.Name.Should().Be("Salkım Domates");
            product.Category.Should().Be("Sebze/Meyve");
            product.DefaultUnit.Should().Be(UnitOfMeasure.Kilogram);
        }
    }

    [Fact]
    public async Task DeactivateProduct_ExcludesFromOnlyActiveList()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        Guid passiveId;
        await using (var ctx = CreateContext(stub, dbName))
        {
            var aktif = Product.Create(tenantId, "Biber", null, UnitOfMeasure.Kilogram).Value;
            var pasif = Product.Create(tenantId, "Patlıcan", null, UnitOfMeasure.Crate).Value;
            ctx.Products.AddRange(aktif, pasif);
            await ctx.SaveChangesAsync();
            passiveId = pasif.Id;
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new DeactivateProductHandler(new ProductRepository(ctx), ctx);
            var result = await handler.Handle(new DeactivateProductCommand(passiveId), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var list = new ListProductsHandler(new ProductRepository(ctx));
            var active = await list.Handle(new ListProductsQuery(1, 20, OnlyActive: true), CancellationToken.None);
            active.Value.Items.Should().ContainSingle().Which.Name.Should().Be("Biber");

            var all = await list.Handle(new ListProductsQuery(1, 20, OnlyActive: false), CancellationToken.None);
            all.Value.TotalCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task GetProduct_NotFound_Fails()
    {
        var dbName = Guid.NewGuid().ToString();
        var stub = new StubTenantContext { TenantId = Guid.NewGuid() };

        await using var ctx = CreateContext(stub, dbName);
        var handler = new GetProductHandler(new ProductRepository(ctx));
        var result = await handler.Handle(new GetProductQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }

    [Fact]
    public async Task ListProducts_ReturnsOnlyCurrentTenant_OrderedByName()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.Products.Add(Product.Create(tenantA, "Zeytin", null, UnitOfMeasure.Kilogram).Value);
            seed.Products.Add(Product.Create(tenantA, "Armut", null, UnitOfMeasure.Crate).Value);
            seed.Products.Add(Product.Create(tenantB, "Başka", null, UnitOfMeasure.Crate).Value);
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stubA, dbName))
        {
            var handler = new ListProductsHandler(new ProductRepository(ctx));
            var result = await handler.Handle(new ListProductsQuery(1, 20, true), CancellationToken.None);

            result.Value.TotalCount.Should().Be(2);
            result.Value.Items.Select(p => p.Name).Should().ContainInOrder("Armut", "Zeytin");
        }
    }
}
