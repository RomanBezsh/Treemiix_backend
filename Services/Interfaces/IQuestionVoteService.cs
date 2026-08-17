using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IQuestionVoteService
{
    Task VoteAsync(Guid userId, VoteRequest request);
    Task<bool> RemoveVoteAsync(Guid id);
}