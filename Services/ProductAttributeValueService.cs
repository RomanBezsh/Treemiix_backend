using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductAttributeValueService : IProductAttributeValueService
{
    private readonly AppDbContext _context;

    public ProductAttributeValueService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductAttributeValue>> GetByProductAsync(Guid productId)
    {
        return await _context.ProductAttributeValues
            .Where(a => a.ProductId == productId)
            .ToListAsync();
    }

    public async Task<ProductAttributeValue> CreateAsync(CreateAttributeRequest request)
    {
        var attribute = new ProductAttributeValue
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            NameAttr = request.NameAttr,
            Value = request.Value
        };

        _context.ProductAttributeValues.Add(attribute);
        await _context.SaveChangesAsync();

        return attribute;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateAttributeRequest request)
    {
        var attribute = await _context.ProductAttributeValues.FindAsync(id);
        if (attribute == null)
            return false;

        attribute.NameAttr = request.NameAttr;
        attribute.Value = request.Value;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var attribute = await _context.ProductAttributeValues.FindAsync(id);
        if (attribute == null)
            return false;

        _context.ProductAttributeValues.Remove(attribute);
        await _context.SaveChangesAsync();

        return true;
    }
}