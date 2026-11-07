using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloneAmazonBack.Tests;

public class SellerServiceTests
{
    private static SellerService CreateService(AppDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seller:CommissionRate"] = "0.10",
                ["Seller:Subscription:PeriodDays"] = "30"
            })
            .Build();

        return new SellerService(
            context,
            configuration,
            NullLogger<SellerService>.Instance);
    }

    private static async Task<(User User, UserRole SellerRole)> SeedUserAsync(
        AppDbContext context,
        string email = "seller@example.com",
        bool emailConfirmed = true)
    {
        var userRole = new UserRole { Id = Guid.NewGuid(), Name = Roles.User, Rights = 1 };
        var sellerRole = new UserRole { Id = Guid.NewGuid(), Name = Roles.Seller, Rights = 3 };

        context.UserRoles.AddRange(userRole, sellerRole);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = "hash",
            FirstName = "Test",
            LastName = "Seller",
            IsActive = true,
            EmailConfirmed = emailConfirmed,
            UserRoleId = userRole.Id
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (user, sellerRole);
    }

    private static async Task<Seller> CompleteOnboardingAsync(SellerService service, Guid userId)
    {
        await service.StartOnboardingAsync(userId);

        await service.UpdateBusinessAsync(userId, new UpdateSellerBusinessRequest(
            SellerBusinessType.Company,
            "Test LLC",
            "US",
            "TAX-123",
            "1 Main Street",
            "support@example.com",
            "+10000000000"));

        await service.UpdateStorefrontAsync(userId, new UpdateSellerStorefrontRequest(
            "Test Store",
            "test-store",
            null,
            "Test store description"));

        await service.UpdatePayoutAsync(userId, new UpdateSellerPayoutRequest("BANK-123"));

        return await service.SubmitAsync(userId);
    }

    [Fact]
    public async Task StartOnboarding_ShouldCreateDraftAndAssignSellerRole()
    {
        using var context = TestDbContextFactory.Create();
        var (user, sellerRole) = await SeedUserAsync(context);
        var service = CreateService(context);

        var seller = await service.StartOnboardingAsync(user.Id);

        Assert.Equal(SellerStatus.Draft, seller.Status);
        Assert.Equal(1, seller.OnboardingStep);
        Assert.Equal(sellerRole.Id, context.Users.Single(u => u.Id == user.Id).UserRoleId);
        Assert.Single(context.Sellers);
    }

    [Fact]
    public async Task StartOnboarding_ShouldRequireConfirmedEmail()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context, emailConfirmed: false);
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartOnboardingAsync(user.Id));
    }

    [Fact]
    public async Task Submit_ShouldSetPendingVerification()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);

        var seller = await CompleteOnboardingAsync(service, user.Id);

        Assert.Equal(SellerStatus.PendingVerification, seller.Status);
        Assert.NotNull(seller.SubmittedAt);
        Assert.Equal(4, seller.OnboardingStep);
    }

    [Fact]
    public async Task Submit_WithoutRequiredData_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);
        await service.StartOnboardingAsync(user.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(user.Id));
    }

    [Fact]
    public async Task Approve_ShouldActivateSellerAndStartSubscription()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);
        var seller = await CompleteOnboardingAsync(service, user.Id);

        var approved = await service.ApproveAsync(seller.Id);

        Assert.Equal(SellerStatus.Active, approved.Status);
        Assert.Equal(SellerSubscriptionStatus.Active, approved.SubscriptionStatus);
        Assert.True(approved.CanSell);
        Assert.True(approved.SubscriptionPaidUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task Reject_ShouldStoreReasonAndAllowResubmission()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);
        var seller = await CompleteOnboardingAsync(service, user.Id);

        var rejected = await service.RejectAsync(seller.Id, "Invalid tax number");

        Assert.Equal(SellerStatus.Rejected, rejected.Status);
        Assert.Equal("Invalid tax number", rejected.RejectionReason);

        // A rejected seller may edit the application and submit it again.
        await service.UpdateBusinessAsync(user.Id, new UpdateSellerBusinessRequest(
            SellerBusinessType.Company,
            "Test LLC",
            "US",
            "TAX-456",
            "1 Main Street",
            "support@example.com",
            "+10000000000"));

        var resubmitted = await service.SubmitAsync(user.Id);
        Assert.Equal(SellerStatus.PendingVerification, resubmitted.Status);
    }

    [Fact]
    public async Task MarkSubscriptionPaid_ShouldExtendPaidUntil()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);
        var seller = await CompleteOnboardingAsync(service, user.Id);
        await service.ApproveAsync(seller.Id);

        var paidUntil = DateTime.UtcNow.AddDays(60);
        var updated = await service.MarkSubscriptionPaidAsync(seller.Id, paidUntil);

        Assert.Equal(paidUntil, updated.SubscriptionPaidUntil);
        Assert.Equal(SellerSubscriptionStatus.Active, updated.SubscriptionStatus);
    }

    [Fact]
    public async Task MarkSubscriptionPaid_WithoutDate_ShouldExtendCurrentPeriod()
    {
        using var context = TestDbContextFactory.Create();
        var (user, _) = await SeedUserAsync(context);
        var service = CreateService(context);
        var seller = await CompleteOnboardingAsync(service, user.Id);
        await service.ApproveAsync(seller.Id);

        var currentPaidUntil = seller.SubscriptionPaidUntil!.Value;
        var updated = await service.MarkSubscriptionPaidAsync(seller.Id, null);

        Assert.Equal(currentPaidUntil.AddDays(30), updated.SubscriptionPaidUntil);
        Assert.Equal(SellerSubscriptionStatus.Active, updated.SubscriptionStatus);
    }

    [Fact]
    public async Task Storefront_WithDuplicateSlug_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var (firstUser, _) = await SeedUserAsync(context, "first@example.com");
        var (secondUser, _) = await SeedUserAsync(context, "second@example.com");
        var service = CreateService(context);

        await service.StartOnboardingAsync(firstUser.Id);
        await service.UpdateStorefrontAsync(firstUser.Id, new UpdateSellerStorefrontRequest(
            "First Store", "first-store", null, null));

        await service.StartOnboardingAsync(secondUser.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStorefrontAsync(secondUser.Id, new UpdateSellerStorefrontRequest(
                "Second Store", "first-store", null, null)));
    }
}
