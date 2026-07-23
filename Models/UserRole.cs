namespace CloneAmazonBack.Models;

public class UserRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rights { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
