using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductAttributeValuesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductAttributeValuesController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var attributes = await _context.ProductAttributeValues
            .Where(a => a.ProductId == productId)
            .ToListAsync();

        return Ok(attributes);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAttributeRequest request)
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

        return Created($"/api/productattributevalues/byproduct/{attribute.ProductId}", attribute);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateAttributeRequest request)
    {
        var attribute = await _context.ProductAttributeValues.FindAsync(id);
        if (attribute == null) return NotFound();

        attribute.NameAttr = request.NameAttr;
        attribute.Value = request.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var attribute = await _context.ProductAttributeValues.FindAsync(id);
        if (attribute == null) return NotFound();

        _context.ProductAttributeValues.Remove(attribute);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
