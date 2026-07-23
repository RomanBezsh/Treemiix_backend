namespace CloneAmazonBack.Models;

public enum SellerStatus
{
    Active,
    Inactive,
    Suspended,
    PendingVerification
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
