using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync(Guid userId, Guid? sellerId);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order> CreateAsync(Guid userId, CreateOrderRequest request);
    Task UpdateStatusAsync(Guid id, OrderStatus status);
}