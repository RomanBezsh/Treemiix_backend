using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

/// <summary>
/// Checkout of the signed-in user's cart: the items come from the cart, the server
/// groups them by seller and creates one order per seller.
/// </summary>
public record CheckoutRequest(
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

    [param: StringLength(50)]
    string? PromoCode
);

public record CheckoutOrderSummary(
    Guid Id,
    Guid SellerId,
    string StoreName,
    int ItemsCount,
    decimal TotalAmount,
    decimal DiscountAmount
);

/// <summary>Created orders and the totals of the whole checkout.</summary>
public record CheckoutResponse(
    List<CheckoutOrderSummary> Orders,
    decimal Subtotal,
    decimal Discount,
    decimal Total
);
