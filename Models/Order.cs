namespace CloneAmazonBack.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SellerId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Seller Seller { get; set; } = null!;
    public PromoCode? PromoCode { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string ProductAvatarUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
