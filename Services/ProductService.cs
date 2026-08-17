using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync(Guid? categoryId, Guid? sellerId, bool? isActive)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .WhereIf(categoryId, p => p.CategoryId == categoryId!.Value)
            .WhereIf(sellerId, p => p.SellerId == sellerId!.Value)
            .WhereIf(isActive, p => p.IsActive == isActive!.Value)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Include(p => p.Galleries)
            .Include(p => p.AttributeValues)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            SellerId = request.SellerId,
            CategoryId = request.CategoryId,
            Price = request.Price,
            OldCost = request.OldCost,
            Stock = request.Stock,
            Description = request.Description,
            Sku = request.Sku,
            IsActive = true,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        if (product.Price > 0)
        {
            _context.HistoryPriceProducts.Add(new HistoryPriceProduct
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Price = product.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return product;
    }

    public async Task UpdateAsync(Guid id, CreateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return;

        var priceChanged = product.Price != request.Price;

        product.Name = request.Name;
        product.Slug = request.Slug;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.OldCost = request.OldCost;
        product.Stock = request.Stock;
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.UpdatedAt = DateTime.UtcNow;

        if (priceChanged && request.Price > 0)
        {
            _context.HistoryPriceProducts.Add(new HistoryPriceProduct
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return;

        product.IsActive = false;
        product.Status = ProductStatus.Archived;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}