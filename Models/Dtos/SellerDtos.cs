using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record UpdateSellerBusinessRequest(
    SellerBusinessType BusinessType,

    [param: Required]
    [param: StringLength(200, MinimumLength = 2)]
    string LegalName,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string Country,

    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string TaxNumber,

    [param: Required]
    [param: StringLength(300, MinimumLength = 5)]
    string LegalAddress,

    [param: EmailAddress]
    [param: StringLength(200)]
    string? SupportEmail,

    [param: Phone]
    [param: StringLength(30)]
    string? SupportPhone
);

public record UpdateSellerStorefrontRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string StoreName,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string StoreSlug,

    [param: StringLength(500)]
    string? LogoUrl,

    [param: StringLength(2000)]
    string? Description
);

public record UpdateSellerPayoutRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 5)]
    string BankAccount
);

public record RejectSellerRequest(
    [param: Required]
    [param: StringLength(500, MinimumLength = 3)]
    string Reason
);

public record MarkSellerSubscriptionPaidRequest(
    DateTime? PaidUntil
);

public record SellerPlanResponse(
    string Name,
    decimal MonthlyPrice,
    string Currency,
    int PeriodDays
);

public record SellerPrivateResponse(
    Guid Id,
    Guid UserId,
    string StoreName,
    string StoreSlug,
    string? LogoUrl,
    string? Description,
    SellerBusinessType BusinessType,
    string? LegalName,
    string? Country,
    string? SupportEmail,
    string? SupportPhone,
    string? TaxNumber,
    string? LegalAddress,
    string? BankAccount,
    decimal Rating,
    SellerStatus Status,
    decimal CommissionRate,
    int OnboardingStep,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectionReason,
    SellerSubscriptionStatus SubscriptionStatus,
    DateTime? SubscriptionPaidUntil,
    bool CanSell
);

public record AdminSellerResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string StoreName,
    string StoreSlug,
    string? LogoUrl,
    string? Description,
    SellerBusinessType BusinessType,
    string? LegalName,
    string? Country,
    string? SupportEmail,
    string? SupportPhone,
    string? TaxNumber,
    string? LegalAddress,
    string? BankAccount,
    decimal Rating,
    SellerStatus Status,
    decimal CommissionRate,
    int OnboardingStep,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectionReason,
    SellerSubscriptionStatus SubscriptionStatus,
    DateTime? SubscriptionPaidUntil,
    bool CanSell
);
