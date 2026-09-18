using CloneAmazonBack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CloneAmazonBack.Data;

public static class DbSeeder
{
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
        await SeedCategoriesAndSellersAsync(context);
        await SeedProductsAsync(context);
    }

    public static async Task SeedProductsAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var seller = await context.Sellers.FirstOrDefaultAsync();
        var category = await context.Categories.FirstOrDefaultAsync();

        if (seller == null || category == null) return;

        var products = new List<Product>();
        for (int i = 1; i <= 20; i++)
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
        // Временно даем права админа пользователю по ID
        var myUserId = Guid.Parse("c2aa3bf8-4020-4831-bd61-56da3689dc12");
        var myUser = await context.Users.FindAsync(myUserId);
        
        if (myUser != null)
        {
            var adminRole = await context.UserRoles.FirstOrDefaultAsync(r => r.Name == Roles.Admin);
            if (adminRole != null)
            {
                myUser.UserRoleId = adminRole.Id;
                await context.SaveChangesAsync();
            }
        }

        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
            return;

        var defaultAdminRole = await context.UserRoles
            .FirstOrDefaultAsync(r => r.Name == Roles.Admin);

        if (defaultAdminRole is null)
            return;

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FirstName = "Admin",
            LastName = "Admin",
            IsActive = true,
            UserRoleId = defaultAdminRole.Id
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAndSellersAsync(AppDbContext context)
    {
        // 1. Seed Categories if empty
        if (!await context.Categories.AnyAsync())
        {
            var cat1 = new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000001"),
                Name = "Electronics",
                Slug = "electronics",
                Path = "Electronics",
                SortOrder = 1,
                IsActive = true
            };
            var cat2 = new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000002"),
                Name = "Home & Kitchen",
                Slug = "home-kitchen",
                Path = "Home & Kitchen",
                SortOrder = 2,
                IsActive = true
            };
            var cat3 = new Category
            {
                Id = Guid.Parse("e1b10000-0000-0000-0000-000000000003"),
                Name = "Footwear & Sports",
                Slug = "footwear-sports",
                Path = "Footwear & Sports",
                SortOrder = 3,
                IsActive = true
            };

            context.Categories.AddRange(cat1, cat2, cat3);
            await context.SaveChangesAsync();
        }

        // 2. Seed Seller if user c2aa3bf8-4020-4831-bd61-56da3689dc12 exists and seller doesn't exist
        var myUserId = Guid.Parse("c2aa3bf8-4020-4831-bd61-56da3689dc12");
        var myUser = await context.Users.FindAsync(myUserId);
        if (myUser != null)
        {
            var sellerExists = await context.Sellers.AnyAsync(s => s.UserId == myUserId);
            if (!sellerExists)
            {
                var seller = new Seller
                {
                    Id = Guid.Parse("e1b10000-0000-0000-0000-000000000010"),
                    UserId = myUserId,
                    StoreName = "Treemiix Official Store",
                    StoreSlug = "treemiix-official",
                    Rating = 5.0m,
                    Status = SellerStatus.Active,
                    CommissionRate = 0.10m,
                    CreatedAt = DateTime.UtcNow
                };

                context.Sellers.Add(seller);
                await context.SaveChangesAsync();
            }
        }
    }
}
