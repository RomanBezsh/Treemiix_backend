using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductQuestionService : IProductQuestionService
{
    private readonly AppDbContext _context;

    public ProductQuestionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductQuestion>> GetByProductAsync(Guid productId)
    {
        return await _context.ProductQuestions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .Where(q => q.ProductId == productId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductQuestion?> GetByIdAsync(Guid id)
    {
        return await _context.ProductQuestions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<ProductQuestion> CreateAsync(Guid userId, CreateQuestionRequest request)
    {
        var question = new ProductQuestion
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductQuestions.Add(question);
        await _context.SaveChangesAsync();

        return question;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin = false)
    {
        var question = await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == id && (q.UserId == userId || isAdmin));
        if (question == null)
            return false;

        _context.ProductQuestions.Remove(question);
        await _context.SaveChangesAsync();

        return true;
    }
}