using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services;

namespace CloneAmazonBack.Tests;

public class OrderServiceTests
{
    private static OrderService CreateService(Data.AppDbContext context)
    {
        return new OrderService(context);
    }

    private static User CreateUser(Data.AppDbContext context)
    {
        var role = new UserRole { Id = Guid.NewGuid(), Name = "User", Rights = 1 };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Password = "hashed",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            UserRoleId = role.Id,
            UserRole = role
        };

        context.UserRoles.Add(role);
        context.Users.Add(user);
        return user;
    }

    private static Product CreateProduct(Data.AppDbContext context, string name, decimal price, int stock = 100)
    {
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StoreName = "Seller",
            StoreSlug = "seller",
            Description = "D",
            Status = SellerStatus.Active
        };
        var category = new Category { Id = Guid.NewGuid(), Name = "Category", Slug = "category" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            SellerId = seller.Id,
            CategoryId = category.Id,
            Price = price,
            Stock = stock,
            IsActive = true,
            Status = ProductStatus.Active
        };

        context.Sellers.Add(seller);
        context.Categories.Add(category);
        context.Products.Add(product);
        return product;
    }

    [Fact]
    public async Task CreateAsync_ShouldCalculateTotalAmount()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var productA = CreateProduct(context, "Product A", 10.50m);
        var productB = CreateProduct(context, "Product B", 5.25m);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: null,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: productA.Id, Quantity: 2),
                new(ProductId: productB.Id, Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(26.25m, order.TotalAmount);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_WithPromoCode_ShouldApplyDiscount()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var product = CreateProduct(context, "Product A", 100m);

        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = "SALE10",
            DiscountValue = 10m,
            DiscountType = DiscountType.Percentage,
            MaxActivations = 100,
            LimitPerUser = 1,
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        context.PromoCodes.Add(promo);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: promo.Id,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: product.Id, Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        Assert.Equal(100m, order.TotalAmount);
        Assert.Equal(10m, order.DiscountAmount);
    }

    [Fact]
    public async Task CreateAsync_WithInactivePromoCode_ShouldNotApplyDiscount()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var product = CreateProduct(context, "Product A", 100m);

        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = "INACTIVE",
            DiscountValue = 10m,
            DiscountType = DiscountType.Percentage,
            MaxActivations = 100,
            LimitPerUser = 1,
            IsActive = false,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        context.PromoCodes.Add(promo);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: promo.Id,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: product.Id, Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        Assert.Equal(0m, order.DiscountAmount);
    }

    [Fact]
    public async Task CreateAsync_ShouldDecrementStock()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var product = CreateProduct(context, "Product A", 10m, stock: 5);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: null,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: product.Id, Quantity: 2)
            });

        await service.CreateAsync(user.Id, request);

        var updated = await context.Products.FindAsync(product.Id);
        Assert.Equal(3, updated!.Stock);
    }

    [Fact]
    public async Task CreateAsync_WithInsufficientStock_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var product = CreateProduct(context, "Product A", 10m, stock: 1);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: null,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: product.Id, Quantity: 5)
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(user.Id, request));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownProduct_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: null,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: Guid.NewGuid(), Quantity: 1)
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(user.Id, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldChangeOrderStatus()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var user = CreateUser(context);
        var product = CreateProduct(context, "Product A", 10m);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest(
            SellerId: Guid.NewGuid(),
            PromoCodeId: null,
            ShippingAddress: "123 Main St, Springfield",
            ReceiverName: "John Doe",
            ReceiverPhone: "+1234567890",
            Items: new List<CreateOrderItemRequest>
            {
                new(ProductId: product.Id, Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        await service.UpdateStatusAsync(order.Id, OrderStatus.Shipped);

        var updated = await context.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, updated!.Status);
    }
}