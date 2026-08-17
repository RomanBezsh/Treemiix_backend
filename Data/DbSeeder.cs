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
            UserRoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }
}
