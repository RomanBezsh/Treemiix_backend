using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories
            .Include(c => c.Children)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category> CreateAsync(CreateCategoryRequest request)
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

        return category;
    }

    public async Task UpdateAsync(Guid id, CreateCategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return;

        category.ParentId = request.ParentId;
        category.Name = request.Name;
        category.Slug = request.Slug;
        category.Path = await _context.BuildCategoryPathAsync(request.ParentId, request.Slug);
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}