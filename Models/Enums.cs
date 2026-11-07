using System.Text.Json.Serialization;

namespace CloneAmazonBack.Models;

// Seller enums are serialized as strings to make the onboarding API friendlier
// for the frontend. Other (older) enums keep the default numeric representation.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerStatus
{
    Active,
    Inactive,
    Suspended,
    PendingVerification,
    Draft,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerBusinessType
{
    Individual,
    SoleProprietor,
    Company
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SellerSubscriptionStatus
{
    None,
    Active,
    PastDue,
    Canceled
}

public enum ProductStatus
{
    Active,
    Inactive,
    Archived,
    Deleted
}

public enum DiscountType
{
    Percentage,
    FixedAmount
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}
