namespace CloneAmazonBack.Models.Dtos;

// Flat read models for GET /api/products. Returning EF entities here made
// System.Text.Json walk Seller -> Products -> Category -> Products ... and fail.

public record ProductListItemResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid SellerId,
    Guid CategoryId,
    decimal Price,
    decimal? OldCost,
    int Rating,
    int Stock,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ProductStatus Status,
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
    ProductSellerSummary Seller,
    ProductCategorySummary Category,
    List<ProductGalleryResponse> Galleries,
    List<ProductAttributeValueResponse> AttributeValues
);

public record ProductSellerSummary(
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
    decimal Rating,
    SellerStatus Status,
    decimal CommissionRate,
    int OnboardingStep,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectionReason,
    SellerSubscriptionStatus SubscriptionStatus,
    DateTime? SubscriptionPaidUntil,
    DateTime CreatedAt
);

public record ProductCategorySummary(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string Path,
    int SortOrder,
    bool IsActive
);

public record ProductGalleryResponse(
    Guid Id,
    Guid ProductId,
    string Path,
    int SortOrder,
    bool IsMain
);

public record ProductAttributeValueResponse(
    Guid Id,
    Guid ProductId,
    string NameAttr,
    string Value
);
