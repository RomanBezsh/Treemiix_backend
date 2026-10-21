using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync(Guid userId, Guid? sellerId);
    Task<List<Order>> GetForSellerAsync(Guid sellerUserId);
    Task<Order?> GetByIdAsync(Guid id, Guid userId);
    Task<Order> CreateAsync(Guid userId, CreateOrderRequest request);
    Task UpdateStatusAsync(Guid id, OrderStatus status, Guid? requesterUserId = null, bool isAdmin = false);
}