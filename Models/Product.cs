namespace CloneAmazonBack.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid SellerId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal? OldCost { get; set; }
    public int Rating { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ProductStatus Status { get; set; }
    public string? Sku { get; set; }

    public Seller Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;

    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
    public ICollection<ProductGallery> Galleries { get; set; } = new List<ProductGallery>();
    public ICollection<ProductVideo> Videos { get; set; } = new List<ProductVideo>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<PromoCodeProduct> PromoCodeProducts { get; set; } = new List<PromoCodeProduct>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<ProductQuestion> Questions { get; set; } = new List<ProductQuestion>();
    public ICollection<HistoryPriceProduct> PriceHistory { get; set; } = new List<HistoryPriceProduct>();
}

public class ProductAttributeValue
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string NameAttr { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public Product Product { get; set; } = null!;
}

public class ProductGallery
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}

public class ProductVideo
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}

public class ProductReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductGalleryId { get; set; }
    public Guid? ProductVideoId { get; set; }
    public int LikesCount { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductGallery? Gallery { get; set; }
    public ProductVideo? Video { get; set; }
}
