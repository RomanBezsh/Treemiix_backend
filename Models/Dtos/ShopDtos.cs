namespace CloneAmazonBack.Models.Dtos;

public record CreateSellerRequest(
    Guid UserId,
    string StoreName,
    string StoreSlug,
    string? LogoUrl,
    string? Description,
    string? TaxNumber,
    string? LegalAddress,
    string? BankAccount,
    decimal CommissionRate
);

public record CreateCategoryRequest(
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive
);

public record CreateProductRequest(
    string Name,
    string Slug,
    Guid SellerId,
    Guid CategoryId,
    decimal Price,
    decimal? OldCost,
    int Stock,
    string Description,
    string? Sku
);

public record CreateCartItemRequest(
    Guid CartId,
    Guid ProductId,
    int Quantity,
    decimal Price
);

public record CreatePromoCodeRequest(
    string Code,
    decimal DiscountValue,
    DiscountType DiscountType,
    decimal? MinOrderAmount,
    decimal? MaxDiscountAmount,
    int MaxActivations,
    int LimitPerUser,
    DateTime StartsAt,
    DateTime ExpiresAt
);

public record CreateOrderRequest(
    Guid UserId,
    Guid SellerId,
    Guid? PromoCodeId,
    string ShippingAddress,
    string ReceiverName,
    string ReceiverPhone,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid? ProductId,
    string ProductName,
    decimal ProductPrice,
    string ProductAvatarUrl,
    int Quantity
);

public record CreateGiftCardRequest(
    decimal InitialBalance,
    Guid PurchasedByUserId,
    DateTime? ExpiresAt
);

public record CreateQuestionRequest(
    Guid ProductId,
    Guid UserId,
    string Content
);

public record CreateAnswerRequest(
    Guid QuestionId,
    Guid UserId,
    string Content,
    bool IsOfficialAnswer
);

public record VoteRequest(
    Guid QuestionId,
    Guid UserId,
    short Value
);
