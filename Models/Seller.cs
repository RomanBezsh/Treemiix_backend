namespace CloneAmazonBack.Models;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Seller
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    public SellerBusinessType BusinessType { get; set; }
    public string? LegalName { get; set; }
    public string? Country { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }

    // Sensitive legal/financial data is never serialized to API clients.
    [JsonIgnore]
    public string? TaxNumber { get; set; }

    [JsonIgnore]
    public string? LegalAddress { get; set; }

    [JsonIgnore]
    public string? BankAccount { get; set; }

    public decimal Rating { get; set; }
    public SellerStatus Status { get; set; }
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// 1 = business details, 2 = storefront, 3 = payout details, 4 = submitted.
    /// Used by the frontend to resume the onboarding wizard.
    /// </summary>
    public int OnboardingStep { get; set; } = 1;

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    // MVP billing: the administrator marks the monthly subscription as paid.
    // Stripe can replace this later without changing the rest of the seller flow.
    public SellerSubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? SubscriptionPaidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>
    /// A seller may publish products only after approval and while the subscription is paid.
    /// </summary>
    [NotMapped]
    public bool CanSell =>
        Status == SellerStatus.Active &&
        SubscriptionStatus == SellerSubscriptionStatus.Active &&
        (SubscriptionPaidUntil is null || SubscriptionPaidUntil > DateTime.UtcNow);
}
