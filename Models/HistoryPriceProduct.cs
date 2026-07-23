namespace CloneAmazonBack.Models;

public class HistoryPriceProduct
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
