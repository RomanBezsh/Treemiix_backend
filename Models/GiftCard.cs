namespace CloneAmazonBack.Models;

public class GiftCard
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }

    public Guid PurchasedByUserId { get; set; }
    public Guid? ActivatedByUserId { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public User PurchasedByUser { get; set; } = null!;
    public User? ActivatedByUser { get; set; }
}
