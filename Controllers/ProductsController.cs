using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId, [FromQuery] Guid? sellerId, [FromQuery] bool? isActive)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .WhereIf(categoryId, p => p.CategoryId == categoryId!.Value)
            .WhereIf(sellerId, p => p.SellerId == sellerId!.Value)
            .WhereIf(isActive, p => p.IsActive == isActive!.Value)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Include(p => p.Galleries)
            .Include(p => p.AttributeValues)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
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
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name = request.Name;
        product.Slug = request.Slug;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.OldCost = request.OldCost;
        product.Stock = request.Stock;
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive = false;
        product.Status = ProductStatus.Archived;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
