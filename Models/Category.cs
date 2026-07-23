namespace CloneAmazonBack.Models;

public class Category
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
