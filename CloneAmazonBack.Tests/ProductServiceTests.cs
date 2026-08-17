using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services;

namespace CloneAmazonBack.Tests;

public class ProductServiceTests
{
    private static ProductService CreateService(Data.AppDbContext context)
    {
        return new ProductService(context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProductAndPriceHistory()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var request = new CreateProductRequest(
            Name: "Test Product",
            Slug: "test-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 99.99m,
            OldCost: 120m,
            Stock: 10,
            Description: "A test product",
            Sku: "SKU-001");

        var product = await service.CreateAsync(request);

        Assert.Equal("Test Product", product.Name);
        Assert.True(product.IsActive);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.Single(context.HistoryPriceProducts);
        Assert.Equal(99.99m, context.HistoryPriceProducts.Single().Price);
    }

    [Fact]
    public async Task CreateAsync_WithZeroPrice_ShouldNotAddPriceHistory()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var request = new CreateProductRequest(
            Name: "Free Product",
            Slug: "free-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 0m,
            OldCost: null,
            Stock: 5,
            Description: "A free product",
            Sku: "SKU-002");

        await service.CreateAsync(request);

        Assert.Empty(context.HistoryPriceProducts);
    }

    [Fact]
    public async Task UpdateAsync_WithPriceChange_ShouldAddPriceHistory()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var createRequest = new CreateProductRequest(
            Name: "Test Product",
            Slug: "test-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 50m,
            OldCost: null,
            Stock: 10,
            Description: "A test product",
            Sku: "SKU-003");

        var product = await service.CreateAsync(createRequest);

        var updateRequest = createRequest with { Price = 75m };
        await service.UpdateAsync(product.Id, updateRequest);

        Assert.Equal(2, context.HistoryPriceProducts.Count());
        Assert.Contains(context.HistoryPriceProducts, h => h.Price == 50m);
        Assert.Contains(context.HistoryPriceProducts, h => h.Price == 75m);
    }

    [Fact]
    public async Task UpdateAsync_WithoutPriceChange_ShouldNotAddPriceHistory()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var createRequest = new CreateProductRequest(
            Name: "Test Product",
            Slug: "test-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 50m,
            OldCost: null,
            Stock: 10,
            Description: "A test product",
            Sku: "SKU-004");

        var product = await service.CreateAsync(createRequest);

        var updateRequest = createRequest with { Name = "Updated Name" };
        await service.UpdateAsync(product.Id, updateRequest);

        Assert.Single(context.HistoryPriceProducts);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldArchiveProduct()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var request = new CreateProductRequest(
            Name: "Test Product",
            Slug: "test-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 50m,
            OldCost: null,
            Stock: 10,
            Description: "A test product",
            Sku: "SKU-005");

        var product = await service.CreateAsync(request);

        await service.SoftDeleteAsync(product.Id);

        var updated = await context.Products.FindAsync(product.Id);
        Assert.False(updated!.IsActive);
        Assert.Equal(ProductStatus.Archived, updated.Status);
    }

    [Fact]
    public async Task HardDeleteAsync_ShouldRemoveProduct()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var request = new CreateProductRequest(
            Name: "Test Product",
            Slug: "test-product",
            SellerId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 50m,
            OldCost: null,
            Stock: 10,
            Description: "A test product",
            Sku: "SKU-006");

        var product = await service.CreateAsync(request);

        await service.HardDeleteAsync(product.Id);

        Assert.Empty(context.Products);
    }
}