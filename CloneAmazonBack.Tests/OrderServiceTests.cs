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

    [Fact]
    public async Task CreateAsync_ShouldCalculateTotalAmount()
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
                new(
                    ProductId: null,
                    ProductName: "Product A",
                    ProductPrice: 10.50m,
                    ProductAvatarUrl: "http://example.com/a.jpg",
                    Quantity: 2),
                new(
                    ProductId: null,
                    ProductName: "Product B",
                    ProductPrice: 5.25m,
                    ProductAvatarUrl: "http://example.com/b.jpg",
                    Quantity: 1)
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
                new(
                    ProductId: null,
                    ProductName: "Product A",
                    ProductPrice: 100m,
                    ProductAvatarUrl: "http://example.com/a.jpg",
                    Quantity: 1)
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
                new(
                    ProductId: null,
                    ProductName: "Product A",
                    ProductPrice: 100m,
                    ProductAvatarUrl: "http://example.com/a.jpg",
                    Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        Assert.Equal(0m, order.DiscountAmount);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldChangeOrderStatus()
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
                new(
                    ProductId: null,
                    ProductName: "Product A",
                    ProductPrice: 10m,
                    ProductAvatarUrl: "http://example.com/a.jpg",
                    Quantity: 1)
            });

        var order = await service.CreateAsync(user.Id, request);

        await service.UpdateStatusAsync(order.Id, OrderStatus.Shipped);

        var updated = await context.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, updated!.Status);
    }
}