using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Tests;

public class CheckoutTests
{
    private static readonly CheckoutRequest Request = new(
        ShippingAddress: "12 Khreshchatyk St, Kyiv",
        ReceiverName: "Liam Smith",
        ReceiverPhone: "+380501112233",
        PromoCode: null);

    private static User CreateUser(AppDbContext context)
    {
        var role = new UserRole { Id = Guid.NewGuid(), Name = "User", Rights = 1 };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "hashed",
            FirstName = "Liam",
            LastName = "Smith",
            IsActive = true,
            UserRoleId = role.Id,
            UserRole = role
        };
        context.UserRoles.Add(role);
        context.Users.Add(user);
        return user;
    }

    private static Seller CreateSeller(AppDbContext context, string storeName, SellerStatus status = SellerStatus.Active)
    {
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StoreName = storeName,
            StoreSlug = storeName.ToLower().Replace(" ", "-"),
            Description = "D",
            Status = status
        };
        context.Sellers.Add(seller);
        return seller;
    }

    private static Product CreateProduct(AppDbContext context, Seller seller, string name, decimal price, int stock = 10, bool isActive = true)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = $"Category {name}", Slug = $"category-{Guid.NewGuid():N}" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            SellerId = seller.Id,
            CategoryId = category.Id,
            Price = price,
            Stock = stock,
            IsActive = isActive,
            Status = ProductStatus.Active
        };
        context.Categories.Add(category);
        context.Products.Add(product);
        return product;
    }

    private static Cart CreateCart(AppDbContext context, User user, params (Product Product, int Quantity)[] items)
    {
        var cart = new Cart { Id = Guid.NewGuid(), UserId = user.Id };
        foreach (var (product, quantity) in items)
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = quantity,
                Price = product.Price
            });
        }
        context.Carts.Add(cart);
        return cart;
    }

    private static PromoCode CreatePromo(
        AppDbContext context,
        string code,
        decimal value = 10m,
        DiscountType type = DiscountType.Percentage,
        int maxActivations = 100,
        int used = 0,
        int limitPerUser = 1,
        decimal? minOrderAmount = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        bool isActive = true)
    {
        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DiscountValue = value,
            DiscountType = type,
            MaxActivations = maxActivations,
            UsedActivationsCount = used,
            LimitPerUser = limitPerUser,
            MinOrderAmount = minOrderAmount,
            StartsAt = startsAt ?? DateTime.UtcNow.AddDays(-1),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
            IsActive = isActive
        };
        context.PromoCodes.Add(promo);
        return promo;
    }

    [Fact]
    public async Task CheckoutAsync_SplitsCartBySeller_DecrementsStock_AndEmptiesCart()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var sellerA = CreateSeller(context, "Store A");
        var sellerB = CreateSeller(context, "Store B");
        var a1 = CreateProduct(context, sellerA, "Lamp", 20m, stock: 5);
        var a2 = CreateProduct(context, sellerA, "Vase", 5m, stock: 5);
        var b1 = CreateProduct(context, sellerB, "Book", 15m, stock: 3);
        CreateCart(context, user, (a1, 2), (a2, 1), (b1, 3));
        await context.SaveChangesAsync();

        var result = await new OrderService(context).CheckoutAsync(user.Id, Request);

        Assert.Equal(2, result.Orders.Count);
        Assert.Equal(90m, result.Subtotal);
        Assert.Equal(0m, result.Discount);
        Assert.Equal(90m, result.Total);

        var orders = await context.Orders.Include(o => o.Items).ToListAsync();
        var orderA = orders.Single(o => o.SellerId == sellerA.Id);
        var orderB = orders.Single(o => o.SellerId == sellerB.Id);
        Assert.Equal(45m, orderA.TotalAmount);
        Assert.Equal(2, orderA.Items.Count);
        Assert.Equal(45m, orderB.TotalAmount);
        Assert.All(orders, o =>
        {
            Assert.Equal(OrderStatus.Pending, o.Status);
            Assert.Equal("Liam Smith", o.ReceiverName);
        });

        Assert.Equal(3, (await context.Products.FindAsync(a1.Id))!.Stock);
        Assert.Equal(4, (await context.Products.FindAsync(a2.Id))!.Stock);
        Assert.Equal(0, (await context.Products.FindAsync(b1.Id))!.Stock);
        Assert.Empty(await context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task CheckoutAsync_EmptyCart_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        CreateCart(context, user);
        await context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new OrderService(context).CheckoutAsync(user.Id, Request));

        Assert.Equal("Your cart is empty.", error.Message);
    }

    [Fact]
    public async Task CheckoutAsync_NotEnoughStock_ThrowsAndChangesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var seller = CreateSeller(context, "Store A");
        var lamp = CreateProduct(context, seller, "Lamp", 20m, stock: 5);
        var book = CreateProduct(context, seller, "Book", 15m, stock: 1);
        CreateCart(context, user, (lamp, 2), (book, 2));
        await context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new OrderService(context).CheckoutAsync(user.Id, Request));

        Assert.Equal("Only 1 left of \"Book\".", error.Message);
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Equal(5, (await context.Products.FindAsync(lamp.Id))!.Stock);
        Assert.Equal(2, await context.CartItems.CountAsync());
    }

    [Fact]
    public async Task CheckoutAsync_ProductOfSuspendedSeller_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var seller = CreateSeller(context, "Store A", SellerStatus.Suspended);
        var lamp = CreateProduct(context, seller, "Lamp", 20m);
        CreateCart(context, user, (lamp, 1));
        await context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new OrderService(context).CheckoutAsync(user.Id, Request));

        Assert.Contains("no longer available", error.Message);
    }

    [Fact]
    public async Task CheckoutAsync_WithPromo_SplitsDiscountAndCountsOneActivation()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var sellerA = CreateSeller(context, "Store A");
        var sellerB = CreateSeller(context, "Store B");
        var lamp = CreateProduct(context, sellerA, "Lamp", 60m);
        var book = CreateProduct(context, sellerB, "Book", 40m);
        CreateCart(context, user, (lamp, 1), (book, 1));
        var promo = CreatePromo(context, "SALE10", value: 10m);
        await context.SaveChangesAsync();

        var result = await new OrderService(context).CheckoutAsync(user.Id, Request with { PromoCode = "sale10" });

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(10m, result.Discount);
        Assert.Equal(90m, result.Total);
        Assert.Equal(10m, result.Orders.Sum(o => o.DiscountAmount));
        Assert.Equal(6m, result.Orders.Single(o => o.SellerId == sellerA.Id).DiscountAmount);
        Assert.Equal(1, (await context.PromoCodes.FindAsync(promo.Id))!.UsedActivationsCount);
        Assert.All(await context.Orders.ToListAsync(), o => Assert.Equal(promo.Id, o.PromoCodeId));
    }

    [Theory]
    [InlineData("expired", "has expired")]
    [InlineData("used-up", "has been fully used")]
    [InlineData("min-amount", "requires an order of at least $500.00")]
    [InlineData("inactive", "is not active")]
    [InlineData("unknown", "was not found")]
    public async Task CheckoutAsync_InvalidPromo_ThrowsReadableError(string kind, string expected)
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var seller = CreateSeller(context, "Store A");
        var lamp = CreateProduct(context, seller, "Lamp", 100m, stock: 5);
        CreateCart(context, user, (lamp, 1));
        switch (kind)
        {
            case "expired":
                CreatePromo(context, "PROMO", expiresAt: DateTime.UtcNow.AddDays(-1), startsAt: DateTime.UtcNow.AddDays(-10));
                break;
            case "used-up":
                CreatePromo(context, "PROMO", maxActivations: 5, used: 5);
                break;
            case "min-amount":
                CreatePromo(context, "PROMO", minOrderAmount: 500m);
                break;
            case "inactive":
                CreatePromo(context, "PROMO", isActive: false);
                break;
        }
        await context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new OrderService(context).CheckoutAsync(user.Id, Request with { PromoCode = "PROMO" }));

        Assert.Contains(expected, error.Message);
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Equal(5, (await context.Products.FindAsync(lamp.Id))!.Stock);
    }

    [Fact]
    public async Task CheckoutAsync_PromoAlreadyUsedByUser_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var user = CreateUser(context);
        var seller = CreateSeller(context, "Store A");
        var lamp = CreateProduct(context, seller, "Lamp", 100m);
        CreateCart(context, user, (lamp, 1));
        var promo = CreatePromo(context, "ONCE", limitPerUser: 1);
        context.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SellerId = seller.Id,
            PromoCodeId = promo.Id,
            ShippingAddress = "Old address",
            ReceiverName = "Liam",
            ReceiverPhone = "+380501112233",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new OrderService(context).CheckoutAsync(user.Id, Request with { PromoCode = "ONCE" }));

        Assert.Equal("You have already used promo code \"ONCE\".", error.Message);
    }

    [Fact]
    public async Task GetAllForAdminAsync_ReturnsOrdersOfAllUsers()
    {
        using var context = TestDbContextFactory.Create();
        var seller = CreateSeller(context, "Store A");
        foreach (var userId in new[] { Guid.NewGuid(), Guid.NewGuid() })
        {
            context.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SellerId = seller.Id,
                ShippingAddress = "Address 1",
                ReceiverName = "Name",
                ReceiverPhone = "+380501112233",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var orders = await new OrderService(context).GetAllForAdminAsync(null);

        Assert.Equal(2, orders.Count);
    }
}
