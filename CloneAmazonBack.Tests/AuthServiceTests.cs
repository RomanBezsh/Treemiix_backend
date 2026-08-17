using CloneAmazonBack.Data;
using CloneAmazonBack.DTOs.Auth;
using CloneAmazonBack.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloneAmazonBack.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService(AppDbContext context, bool requireEmailConfirmation = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestKeyForCloneAmazonBack2026AtLeast32CharsLong!",
                ["Jwt:Issuer"] = "CloneAmazonBack",
                ["Jwt:Audience"] = "CloneAmazonFront",
                ["Jwt:ExpiryHours"] = "24",
                ["Auth:RequireEmailConfirmation"] = requireEmailConfirmation.ToString()
            })
            .Build();

        return new AuthService(
            context,
            configuration,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnToken()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);
        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        Assert.NotNull(result.Token);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal("User", result.User.Role);
        Assert.Single(context.Users);
        Assert.Single(context.UserRoles);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterRequest
            {
                Email = "test@example.com",
                Password = "password123",
                FirstName = "Jane",
                LastName = "Smith"
            }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        });

        Assert.NotNull(result.Token);
        Assert.Equal("test@example.com", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            }));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "unknown@example.com",
                Password = "password123"
            }));
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        var user = context.Users.Single();
        Assert.NotEqual("password123", user.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.Password));
    }

    [Fact]
    public async Task RegisterAsync_WhenConfirmationRequired_ShouldMarkEmailAsUnconfirmed()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context, requireEmailConfirmation: true);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        var user = context.Users.Single();
        Assert.False(user.EmailConfirmed);
        Assert.NotNull(user.EmailConfirmationCode);
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context, requireEmailConfirmation: true);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            }));
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidCode_ShouldConfirmEmail()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context, requireEmailConfirmation: true);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        var code = context.Users.Single().EmailConfirmationCode!;

        await service.ConfirmEmailAsync(new ConfirmEmailRequest
        {
            Email = "test@example.com",
            Code = code
        });

        var user = context.Users.Single();
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailConfirmationCode);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidCode_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context, requireEmailConfirmation: true);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                Email = "test@example.com",
                Code = "000000"
            }));
    }

    [Fact]
    public async Task ConfirmEmailAsync_ThenLogin_ShouldSucceed()
    {
        using var context = TestDbContextFactory.Create();

        var service = CreateService(context, requireEmailConfirmation: true);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        });

        var code = context.Users.Single().EmailConfirmationCode!;

        await service.ConfirmEmailAsync(new ConfirmEmailRequest
        {
            Email = "test@example.com",
            Code = code
        });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        });

        Assert.NotNull(result.Token);
    }
}