using CloneAmazonBack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CloneAmazonBack.Data;

public static class DbSeeder
{
    /// <summary>
    /// Creates the built-in roles and the admin account configured through
    /// <c>Admin:Email</c> / <c>Admin:Password</c>. Safe to call on every startup.
    /// </summary>
    public static async Task SeedRolesAsync(AppDbContext context, IConfiguration configuration)
    {
        var roleNames = new[] { Roles.Admin, Roles.Seller, Roles.User };

        var existingRoles = await context.UserRoles
            .Select(r => r.Name)
            .ToListAsync();

        foreach (var roleName in roleNames)
        {
            if (existingRoles.Contains(roleName))
                continue;

            context.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Rights = roleName == Roles.User ? 1 : 3
            });
        }

        await context.SaveChangesAsync();

        await SeedDefaultAdminAsync(context, configuration);
    }

    /// <summary>
    /// Development-only demo catalog. Never call this against a production database.
    /// </summary>
    public static async Task SeedDemoDataAsync(AppDbContext context)
    {
        await SeedCategoriesAsync(context);
        await SeedDemoSellerAsync(context);
        await SeedProductsAsync(context);
    }

    public static async Task SeedProductsAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var seller = await context.Sellers.FirstOrDefaultAsync();
        var category = await context.Categories.FirstOrDefaultAsync();

        if (seller == null || category == null) return;

        var products = new List<Product>();
        for (var i = 1; i <= 20; i++)
        {
            products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Test Product {i}",
                Slug = $"test-product-{i}",
                Description = $"This is the description for test product number {i}.",
                Price = 10.0m * i,
                Stock = 100,
                SellerId = seller.Id,
                CategoryId = category.Id,
                IsActive = true,
                Status = ProductStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultAdminAsync(AppDbContext context, IConfiguration configuration)
    {
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
            return;

        var adminRole = await context.UserRoles
            .FirstOrDefaultAsync(r => r.Name == Roles.Admin);

        if (adminRole is null)
            return;

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FirstName = "Admin",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true,
            UserRoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        context.Categories.AddRange(
            new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000001"),
                Name = "Electronics",
                Slug = "electronics",
                Path = "Electronics",
                SortOrder = 1,
                IsActive = true
            },
            new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000002"),
                Name = "Home & Kitchen",
                Slug = "home-kitchen",
                Path = "Home & Kitchen",
                SortOrder = 2,
                IsActive = true
            },
            new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000003"),
                Name = "Footwear & Sports",
                Slug = "footwear-sports",
                Path = "Footwear & Sports",
                SortOrder = 3,
                IsActive = true
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoSellerAsync(AppDbContext context)
    {
        if (await context.Sellers.AnyAsync())
            return;

        var sellerRole = await context.UserRoles
            .FirstOrDefaultAsync(r => r.Name == Roles.Seller);

        if (sellerRole is null)
            return;

        var demoUserId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = demoUserId,
            Email = "demo-seller@treemiix.local",
            Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            FirstName = "Demo",
            LastName = "Seller",
            IsActive = true,
            EmailConfirmed = true,
            UserRoleId = sellerRole.Id
        });

        context.Sellers.Add(new Seller
        {
            Id = Guid.NewGuid(),
            UserId = demoUserId,
            StoreName = "Treemiix Official Store",
            StoreSlug = "treemiix-official",
            Rating = 5.0m,
            Status = SellerStatus.Active,
            CommissionRate = 0.10m,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
