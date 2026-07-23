namespace CloneAmazonBack.Models;

public class ProductQuestion
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;
    public int VotesCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<ProductAnswer> Answers { get; set; } = new List<ProductAnswer>();
    public ICollection<QuestionVote> Votes { get; set; } = new List<QuestionVote>();
}

public class ProductAnswer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;
    public bool IsOfficialAnswer { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProductQuestion Question { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class QuestionVote
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public short Value { get; set; }

    public ProductQuestion Question { get; set; } = null!;
    public User User { get; set; } = null!;
}
