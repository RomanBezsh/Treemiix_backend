namespace CloneAmazonBack.Models;

public class Cart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PromoCodeId { get; set; }

    public User User { get; set; } = null!;
    public PromoCode? PromoCode { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
