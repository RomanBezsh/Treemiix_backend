using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductAnswerService : IProductAnswerService
{
    private readonly AppDbContext _context;

    public ProductAnswerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductAnswer>> GetByQuestionAsync(Guid questionId)
    {
        return await _context.ProductAnswers
            .Include(a => a.User)
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.IsOfficialAnswer)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductAnswer> CreateAsync(Guid userId, CreateAnswerRequest request)
    {
        var answer = new ProductAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = request.QuestionId,
            UserId = userId,
            Content = request.Content,
            IsOfficialAnswer = request.IsOfficialAnswer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductAnswers.Add(answer);
        await _context.SaveChangesAsync();

        return answer;
    }

    public async Task<bool> UpdateAsync(Guid id, string content)
    {
        var answer = await _context.ProductAnswers.FindAsync(id);
        if (answer == null)
            return false;

        answer.Content = content;
        answer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var answer = await _context.ProductAnswers.FindAsync(id);
        if (answer == null)
            return false;

        _context.ProductAnswers.Remove(answer);
        await _context.SaveChangesAsync();

        return true;
    }
}