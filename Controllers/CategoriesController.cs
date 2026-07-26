using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Include(c => c.Children)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            ParentId = request.ParentId,
            Name = request.Name,
            Slug = request.Slug,
            Path = await _context.BuildCategoryPathAsync(request.ParentId, request.Slug),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateCategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.ParentId = request.ParentId;
        category.Name = request.Name;
        category.Slug = request.Slug;
        category.Path = await _context.BuildCategoryPathAsync(request.ParentId, request.Slug);
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
