using CloneAmazonBack.Models;

namespace CloneAmazonBack.Services.Interfaces;

public interface IOrderItemService
{
    Task<List<OrderItem>> GetByOrderAsync(Guid orderId, Guid userId);
    Task<OrderItem?> GetByIdAsync(Guid id, Guid userId);
}