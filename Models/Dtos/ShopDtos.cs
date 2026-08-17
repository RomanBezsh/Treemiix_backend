using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateSellerRequest(
    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string StoreName,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string StoreSlug,

    [property: StringLength(500)]
    string? LogoUrl,

    [property: StringLength(2000)]
    string? Description,

    [property: StringLength(50)]
    string? TaxNumber,

    [property: StringLength(300)]
    string? LegalAddress,

    [property: StringLength(100)]
    string? BankAccount,

    [property: Range(typeof(decimal), "0", "100")]
    decimal CommissionRate
);

public record CreateCategoryRequest(
    Guid? ParentId,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string Name,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string Slug,

    [property: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsActive
);

public record CreateProductRequest(
    [property: Required]
    [property: StringLength(200, MinimumLength = 2)]
    string Name,

    [property: Required]
    [property: StringLength(200, MinimumLength = 2)]
    string Slug,

    Guid SellerId,

    Guid CategoryId,

    [property: Range(typeof(decimal), "0.01", "999999999")]
    decimal Price,

    [property: Range(typeof(decimal), "0", "999999999")]
    decimal? OldCost,

    [property: Range(0, int.MaxValue)]
    int Stock,

    [property: Required]
    [property: StringLength(5000, MinimumLength = 1)]
    string Description,

    [property: StringLength(100)]
    string? Sku
);

public record CreateCartItemRequest(
    Guid CartId,

    Guid ProductId,

    [property: Range(1, int.MaxValue)]
    int Quantity
);

public record CreatePromoCodeRequest(
    [property: Required]
    [property: StringLength(50, MinimumLength = 2)]
    string Code,

    [property: Range(typeof(decimal), "0.01", "999999999")]
    decimal DiscountValue,

    DiscountType DiscountType,

    [property: Range(typeof(decimal), "0", "999999999")]
    decimal? MinOrderAmount,

    [property: Range(typeof(decimal), "0", "999999999")]
    decimal? MaxDiscountAmount,

    [property: Range(1, int.MaxValue)]
    int MaxActivations,

    [property: Range(1, int.MaxValue)]
    int LimitPerUser,

    DateTime StartsAt,

    DateTime ExpiresAt
);

public record CreateOrderRequest(
    Guid SellerId,

    Guid? PromoCodeId,

    [property: Required]
    [property: StringLength(500, MinimumLength = 5)]
    string ShippingAddress,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string ReceiverName,

    [property: Required]
    [property: Phone]
    [property: StringLength(30)]
    string ReceiverPhone,

    [property: Required]
    [property: MinLength(1)]
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductId,

    [property: Range(1, int.MaxValue)]
    int Quantity
);

public record CreateGiftCardRequest(
    [property: Range(typeof(decimal), "0.01", "999999999")]
    decimal InitialBalance,

    DateTime? ExpiresAt
);

public record CreateQuestionRequest(
    Guid ProductId,

    [property: Required]
    [property: StringLength(2000, MinimumLength = 2)]
    string Content
);

public record CreateAnswerRequest(
    Guid QuestionId,

    [property: Required]
    [property: StringLength(2000, MinimumLength = 2)]
    string Content,

    bool IsOfficialAnswer
);

public record VoteRequest(
    Guid QuestionId,

    [property: Range(-1, 1)]
    short Value
);