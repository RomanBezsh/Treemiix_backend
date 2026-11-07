using System.Net;
using System.Text.RegularExpressions;
using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class SellerService : ISellerService
{
    private const decimal DefaultCommissionRateFallback = 0.10m;
    private const int DefaultSubscriptionPeriodDaysFallback = 30;

    private static readonly Regex StoreSlugRegex = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "account", "cart", "help", "media", "order", "orders",
        "product", "products", "seller", "sellers", "support", "uploads", "user", "users"
    };

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SellerService> _logger;
    private readonly IEmailSender _emailSender;

    public SellerService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<SellerService> logger,
        IEmailSender? emailSender = null)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailSender = emailSender ?? NullEmailSender.Instance;
    }

    private decimal DefaultCommissionRate =>
        _configuration.GetValue("Seller:CommissionRate", DefaultCommissionRateFallback);

    private int SubscriptionPeriodDays =>
        _configuration.GetValue("Seller:Subscription:PeriodDays", DefaultSubscriptionPeriodDaysFallback);

    public async Task<List<Seller>> GetAllAsync()
    {
        return await _context.Sellers
            .Where(s => s.Status == SellerStatus.Active)
            .OrderByDescending(s => s.Rating)
            .ThenBy(s => s.StoreName)
            .ToListAsync();
    }

    public async Task<List<AdminSellerResponse>> GetAllForAdminAsync(SellerStatus? status)
    {
        var query = _context.Sellers.Include(s => s.User).AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var sellers = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return sellers.Select(ToAdminResponse).ToList();
    }

    public async Task<Seller?> GetByIdAsync(Guid id)
    {
        return await _context.Sellers.FindAsync(id);
    }

    public async Task<AdminSellerResponse?> GetAdminByIdAsync(Guid id)
    {
        var seller = await _context.Sellers
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        return seller is null ? null : ToAdminResponse(seller);
    }

    public async Task<Seller?> GetByUserAsync(Guid userId)
    {
        return await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Seller> StartOnboardingAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            throw new KeyNotFoundException("User was not found");

        if (!user.IsActive)
            throw new InvalidOperationException("Account is disabled");

        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Confirm your email before becoming a seller");

        var sellerRole = await _context.UserRoles
            .FirstOrDefaultAsync(r => r.Name == Roles.Seller);

        if (sellerRole is null)
            throw new InvalidOperationException("Seller role is not configured");

        var existing = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        if (existing is not null)
        {
            EnsureSellerRole(user, sellerRole);
            await _context.SaveChangesAsync();
            return existing;
        }

        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoreName = string.Empty,
            StoreSlug = string.Empty,
            Status = SellerStatus.Draft,
            OnboardingStep = 1,
            CommissionRate = DefaultCommissionRate,
            SubscriptionStatus = SellerSubscriptionStatus.None,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        EnsureSellerRole(user, sellerRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller onboarding started for user {UserId}", userId);
        return seller;
    }

    public async Task<Seller> UpdateBusinessAsync(Guid userId, UpdateSellerBusinessRequest request)
    {
        var seller = await GetOwnedSellerAsync(userId);
        EnsureEditable(seller);

        seller.BusinessType = request.BusinessType;
        seller.LegalName = request.LegalName.Trim();
        seller.Country = request.Country.Trim();
        seller.TaxNumber = request.TaxNumber.Trim();
        seller.LegalAddress = request.LegalAddress.Trim();
        seller.SupportEmail = NormalizeOptional(request.SupportEmail);
        seller.SupportPhone = NormalizeOptional(request.SupportPhone);
        seller.OnboardingStep = Math.Max(seller.OnboardingStep, 2);

        await _context.SaveChangesAsync();
        return seller;
    }

    public async Task<Seller> UpdateStorefrontAsync(Guid userId, UpdateSellerStorefrontRequest request)
    {
        var seller = await GetOwnedSellerAsync(userId);
        EnsureEditable(seller);

        await ApplyStorefrontAsync(seller, request);
        seller.OnboardingStep = Math.Max(seller.OnboardingStep, 3);

        await _context.SaveChangesAsync();
        return seller;
    }

    public async Task<Seller> UpdatePayoutAsync(Guid userId, UpdateSellerPayoutRequest request)
    {
        var seller = await GetOwnedSellerAsync(userId);
        EnsureEditable(seller);

        seller.BankAccount = request.BankAccount.Trim();
        seller.OnboardingStep = Math.Max(seller.OnboardingStep, 4);

        await _context.SaveChangesAsync();
        return seller;
    }

    public async Task<Seller> SubmitAsync(Guid userId)
    {
        var seller = await GetOwnedSellerAsync(userId);
        EnsureEditable(seller);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(seller.StoreName) || string.IsNullOrWhiteSpace(seller.StoreSlug))
            missing.Add("storefront");
        if (string.IsNullOrWhiteSpace(seller.LegalName) || string.IsNullOrWhiteSpace(seller.Country) ||
            string.IsNullOrWhiteSpace(seller.TaxNumber) || string.IsNullOrWhiteSpace(seller.LegalAddress))
            missing.Add("business details");
        if (string.IsNullOrWhiteSpace(seller.BankAccount))
            missing.Add("payout details");

        if (missing.Count > 0)
            throw new InvalidOperationException($"Complete onboarding first: {string.Join(", ", missing)}");

        seller.Status = SellerStatus.PendingVerification;
        seller.SubmittedAt = DateTime.UtcNow;
        seller.RejectedAt = null;
        seller.RejectionReason = null;

        await _context.SaveChangesAsync();

        var email = await GetUserEmailAsync(seller.UserId);
        await TrySendEmailAsync(
            email,
            "Заявка на продавца отправлена — Treemiix",
            $"""
             <p>Здравствуйте!</p>
             <p>Ваша заявка на открытие магазина <b>{WebUtility.HtmlEncode(seller.StoreName)}</b> получена и отправлена на проверку.</p>
             <p>Мы сообщим о результате по этому адресу.</p>
             """);

        _logger.LogInformation("Seller {SellerId} submitted for verification", seller.Id);
        return seller;
    }

    public async Task UpdateAsync(Guid id, UpdateSellerStorefrontRequest request)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller is null)
            throw new KeyNotFoundException("Seller was not found");

        if (seller.Status is SellerStatus.PendingVerification or SellerStatus.Suspended)
            throw new InvalidOperationException("Seller profile cannot be changed in the current status");

        await ApplyStorefrontAsync(seller, request);
        await _context.SaveChangesAsync();
    }

    public async Task<Seller> ApproveAsync(Guid id)
    {
        var seller = await GetSellerForAdminAsync(id);

        if (seller.Status is SellerStatus.Active)
            return seller;

        if (seller.Status is SellerStatus.Draft)
            throw new InvalidOperationException("Seller has not submitted the application yet");

        seller.Status = SellerStatus.Active;
        seller.ApprovedAt = DateTime.UtcNow;
        seller.RejectedAt = null;
        seller.RejectionReason = null;

        // MVP billing: approval starts the first paid period. Renewals are marked by an admin.
        if (seller.SubscriptionStatus != SellerSubscriptionStatus.Active ||
            seller.SubscriptionPaidUntil <= DateTime.UtcNow)
        {
            seller.SubscriptionStatus = SellerSubscriptionStatus.Active;
            seller.SubscriptionPaidUntil = DateTime.UtcNow.AddDays(SubscriptionPeriodDays);
        }

        await _context.SaveChangesAsync();

        var email = await GetUserEmailAsync(seller.UserId);
        await TrySendEmailAsync(
            email,
            "Магазин одобрен — Treemiix",
            $"""
             <p>Здравствуйте!</p>
             <p>Ваш магазин <b>{WebUtility.HtmlEncode(seller.StoreName)}</b> одобрен. Теперь вы можете добавлять товары.</p>
             """);

        _logger.LogInformation("Seller {SellerId} approved", seller.Id);
        return seller;
    }

    public async Task<Seller> RejectAsync(Guid id, string reason)
    {
        var seller = await GetSellerForAdminAsync(id);

        if (seller.Status is not SellerStatus.PendingVerification)
            throw new InvalidOperationException("Only a pending application can be rejected");

        seller.Status = SellerStatus.Rejected;
        seller.RejectedAt = DateTime.UtcNow;
        seller.RejectionReason = reason.Trim();

        await _context.SaveChangesAsync();

        var email = await GetUserEmailAsync(seller.UserId);
        await TrySendEmailAsync(
            email,
            "Заявка на продавца отклонена — Treemiix",
            $"""
             <p>Здравствуйте!</p>
             <p>К сожалению, заявка на магазин <b>{WebUtility.HtmlEncode(seller.StoreName)}</b> отклонена.</p>
             <p>Причина: {WebUtility.HtmlEncode(seller.RejectionReason)}</p>
             <p>Вы можете исправить данные и отправить заявку повторно.</p>
             """);

        _logger.LogInformation("Seller {SellerId} rejected: {Reason}", seller.Id, seller.RejectionReason);
        return seller;
    }

    public async Task<Seller> SuspendAsync(Guid id)
    {
        var seller = await GetSellerForAdminAsync(id);

        if (seller.Status is SellerStatus.Draft or SellerStatus.PendingVerification)
            throw new InvalidOperationException("Only an approved seller can be suspended");

        seller.Status = SellerStatus.Suspended;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller {SellerId} suspended", seller.Id);
        return seller;
    }

    public async Task<Seller> ReactivateAsync(Guid id)
    {
        var seller = await GetSellerForAdminAsync(id);

        if (seller.Status is not (SellerStatus.Suspended or SellerStatus.Inactive))
            throw new InvalidOperationException("Only a suspended or inactive seller can be reactivated");

        seller.Status = SellerStatus.Active;
        seller.ApprovedAt ??= DateTime.UtcNow;

        if (seller.SubscriptionStatus != SellerSubscriptionStatus.Active ||
            seller.SubscriptionPaidUntil <= DateTime.UtcNow)
        {
            seller.SubscriptionStatus = SellerSubscriptionStatus.Active;
            seller.SubscriptionPaidUntil = DateTime.UtcNow.AddDays(SubscriptionPeriodDays);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller {SellerId} reactivated", seller.Id);
        return seller;
    }

    public async Task<Seller> MarkSubscriptionPaidAsync(Guid id, DateTime? paidUntil)
    {
        var seller = await GetSellerForAdminAsync(id);

        var now = DateTime.UtcNow;
        // Renewing before the current period ends must extend it, not shorten it.
        var periodStart = seller.SubscriptionPaidUntil is { } current && current > now
            ? current
            : now;
        var until = paidUntil ?? periodStart.AddDays(SubscriptionPeriodDays);

        if (until <= now)
            throw new InvalidOperationException("Paid until date must be in the future");

        seller.SubscriptionStatus = SellerSubscriptionStatus.Active;
        seller.SubscriptionPaidUntil = until;

        // A seller who was only deactivated because the subscription expired becomes active again.
        if (seller.Status is SellerStatus.Inactive)
        {
            seller.Status = SellerStatus.Active;
            seller.ApprovedAt ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Subscription for seller {SellerId} marked paid until {PaidUntil}",
            seller.Id,
            seller.SubscriptionPaidUntil);
        return seller;
    }

    private async Task<Seller> GetOwnedSellerAsync(Guid userId)
    {
        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        if (seller is null)
            throw new KeyNotFoundException("Seller account was not found");

        return seller;
    }

    private async Task<Seller> GetSellerForAdminAsync(Guid id)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller is null)
            throw new KeyNotFoundException("Seller was not found");

        return seller;
    }

    private static void EnsureEditable(Seller seller)
    {
        if (seller.Status is not (SellerStatus.Draft or SellerStatus.Rejected))
            throw new InvalidOperationException("Seller profile can only be edited before approval or after rejection");
    }

    private async Task ApplyStorefrontAsync(Seller seller, UpdateSellerStorefrontRequest request)
    {
        seller.StoreName = request.StoreName.Trim();
        seller.StoreSlug = await NormalizeAndValidateSlugAsync(seller.Id, request.StoreSlug);
        seller.LogoUrl = NormalizeOptional(request.LogoUrl);
        seller.Description = NormalizeOptional(request.Description);
    }

    private async Task<string> NormalizeAndValidateSlugAsync(Guid sellerId, string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();

        if (!StoreSlugRegex.IsMatch(normalized))
            throw new InvalidOperationException(
                "Store slug may contain only lowercase Latin letters, digits and hyphens");

        if (ReservedSlugs.Contains(normalized))
            throw new InvalidOperationException("This store slug is reserved");

        if (await _context.Sellers.AnyAsync(s => s.Id != sellerId && s.StoreSlug == normalized))
            throw new InvalidOperationException("This store slug is already taken");

        return normalized;
    }

    private static void EnsureSellerRole(User user, UserRole sellerRole)
    {
        // Admins keep their role; seller endpoints already allow the Admin role.
        if (user.UserRole?.Name == Roles.Admin)
            return;

        if (user.UserRoleId != sellerRole.Id)
            user.UserRoleId = sellerRole.Id;
    }

    private async Task<string?> GetUserEmailAsync(Guid userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();
    }

    private async Task TrySendEmailAsync(string? to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        try
        {
            await _emailSender.SendAsync(to, subject, htmlBody);
        }
        catch (Exception ex)
        {
            // Status changes must not fail because the mail provider is temporarily down.
            _logger.LogError(ex, "Failed to send seller email to {Email}", to);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdminSellerResponse ToAdminResponse(Seller seller) => new(
        seller.Id,
        seller.UserId,
        seller.User.Email,
        seller.User.FirstName,
        seller.User.LastName,
        seller.StoreName,
        seller.StoreSlug,
        seller.LogoUrl,
        seller.Description,
        seller.BusinessType,
        seller.LegalName,
        seller.Country,
        seller.SupportEmail,
        seller.SupportPhone,
        seller.TaxNumber,
        seller.LegalAddress,
        seller.BankAccount,
        seller.Rating,
        seller.Status,
        seller.CommissionRate,
        seller.OnboardingStep,
        seller.CreatedAt,
        seller.SubmittedAt,
        seller.ApprovedAt,
        seller.RejectedAt,
        seller.RejectionReason,
        seller.SubscriptionStatus,
        seller.SubscriptionPaidUntil,
        seller.CanSell);
}
