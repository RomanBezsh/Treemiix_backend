using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateSellerRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string StoreName,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string StoreSlug,

    [param: StringLength(500)]
    string? LogoUrl,

    [param: StringLength(2000)]
    string? Description,

    [param: StringLength(50)]
    string? TaxNumber,

    [param: StringLength(300)]
    string? LegalAddress,

    [param: StringLength(100)]
    string? BankAccount,

    [param: Range(typeof(decimal), "0", "100")]
    decimal CommissionRate
);

public record CreateCategoryRequest(
    Guid? ParentId,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string Name,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string Slug,

    [param: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsActive
);

public record CreateProductRequest(
    [param: Required]
    [param: StringLength(200, MinimumLength = 2)]
    string Name,

    [param: Required]
    [param: StringLength(200, MinimumLength = 2)]
    string Slug,

    Guid SellerId,

    Guid CategoryId,

    [param: Range(typeof(decimal), "0.01", "999999999")]
    decimal Price,

    [param: Range(typeof(decimal), "0", "999999999")]
    decimal? OldCost,

    [param: Range(0, int.MaxValue)]
    int Stock,

    [param: Required]
    [param: StringLength(5000, MinimumLength = 1)]
    string Description,

    [param: StringLength(100)]
    string? Sku,

    string? Asin,
    string? ItemModelNumber,
    string? Manufacturer,
    string? CountryOfOrigin,
    string? ProductDimensions,
    string? ItemWeight,
    string? WarrantyInfo,
    string? Features,
    string? Binding,
    string? ReleaseDate,
    string? ImageUrl,
    List<string>? Images
);

public record CreateCartItemRequest(
    Guid CartId,

    Guid ProductId,

    [param: Range(1, int.MaxValue)]
    int Quantity
);

public record CreatePromoCodeRequest(
    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string Code,

    [param: Range(typeof(decimal), "0.01", "999999999")]
    decimal DiscountValue,

    DiscountType DiscountType,

    [param: Range(typeof(decimal), "0", "999999999")]
    decimal? MinOrderAmount,

    [param: Range(typeof(decimal), "0", "999999999")]
    decimal? MaxDiscountAmount,

    [param: Range(1, int.MaxValue)]
    int MaxActivations,

    [param: Range(1, int.MaxValue)]
    int LimitPerUser,

    DateTime StartsAt,

    DateTime ExpiresAt
);

public record CreateOrderRequest(
    Guid SellerId,

    Guid? PromoCodeId,

    [param: Required]
    [param: StringLength(500, MinimumLength = 5)]
    string ShippingAddress,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string ReceiverName,

    [param: Required]
    [param: Phone]
    [param: StringLength(30)]
    string ReceiverPhone,

    [param: Required]
    [param: MinLength(1)]
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductId,

    [param: Range(1, int.MaxValue)]
    int Quantity
);

public record CreateGiftCardRequest(
    [param: Range(typeof(decimal), "0.01", "999999999")]
    decimal InitialBalance,

    DateTime? ExpiresAt
);

public record CreateQuestionRequest(
    Guid ProductId,

    [param: Required]
    [param: StringLength(2000, MinimumLength = 2)]
    string Content
);

public record CreateAnswerRequest(
    Guid QuestionId,

    [param: Required]
    [param: StringLength(2000, MinimumLength = 2)]
    string Content,

    bool IsOfficialAnswer
);

public record VoteRequest(
    Guid QuestionId,

    [param: Range(-1, 1)]
    short Value
);
