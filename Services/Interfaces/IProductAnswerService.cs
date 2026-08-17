using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductAnswerService
{
    Task<List<ProductAnswer>> GetByQuestionAsync(Guid questionId);
    Task<ProductAnswer> CreateAsync(Guid userId, CreateAnswerRequest request);
    Task<bool> UpdateAsync(Guid id, string content, Guid userId, bool isAdmin = false);
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin = false);
}