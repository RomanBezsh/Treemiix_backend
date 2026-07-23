namespace CloneAmazonBack.Models;

public class Seller
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    public string? TaxNumber { get; set; }
    public string? LegalAddress { get; set; }
    public string? BankAccount { get; set; }

    public decimal Rating { get; set; }
    public SellerStatus Status { get; set; }
    public decimal CommissionRate { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
