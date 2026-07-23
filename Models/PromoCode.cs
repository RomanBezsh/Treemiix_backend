namespace CloneAmazonBack.Models;

public class PromoCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DiscountType DiscountType { get; set; }

    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public int MaxActivations { get; set; }
    public int UsedActivationsCount { get; set; }
    public int LimitPerUser { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public ICollection<PromoCodeProduct> PromoCodeProducts { get; set; } = new List<PromoCodeProduct>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class PromoCodeProduct
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid PromoCodeId { get; set; }

    public Product Product { get; set; } = null!;
    public PromoCode PromoCode { get; set; } = null!;
}
