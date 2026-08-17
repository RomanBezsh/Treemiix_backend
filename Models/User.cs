namespace CloneAmazonBack.Models;

public class User
{
    public Guid Id { get; set; }
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationCode { get; set; }
    public DateTime? EmailConfirmationCodeExpiresAt { get; set; }
    public Guid UserRoleId { get; set; }

    public UserRole UserRole { get; set; } = null!;
    public UserProfile? Profile { get; set; }
    public Seller? Seller { get; set; }

    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<ProductQuestion> Questions { get; set; } = new List<ProductQuestion>();
    public ICollection<ProductAnswer> Answers { get; set; } = new List<ProductAnswer>();
    public ICollection<QuestionVote> QuestionVotes { get; set; } = new List<QuestionVote>();
    public ICollection<GiftCard> PurchasedGiftCards { get; set; } = new List<GiftCard>();
    public ICollection<GiftCard> ActivatedGiftCards { get; set; } = new List<GiftCard>();
}

public class UserAddress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
}

public class UserProfile
{
    public Guid UserId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }

    public User User { get; set; } = null!;
}
