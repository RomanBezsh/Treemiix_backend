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
            .Include(p => p.Galleries)
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
            .Include(p => p.Videos)
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
            Asin = request.Asin,
            ItemModelNumber = request.ItemModelNumber,
            Manufacturer = request.Manufacturer,
            CountryOfOrigin = request.CountryOfOrigin,
            ProductDimensions = request.ProductDimensions,
            ItemWeight = request.ItemWeight,
            WarrantyInfo = request.WarrantyInfo,
            Features = request.Features,
            Binding = request.Binding,
            ReleaseDate = request.ReleaseDate,
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

        // Handle Images/Gallery
        if (!string.IsNullOrEmpty(request.ImageUrl) || (request.Images != null && request.Images.Count > 0))
        {
            var imagesList = request.Images ?? new List<string>();
            if (!string.IsNullOrEmpty(request.ImageUrl) && !imagesList.Contains(request.ImageUrl))
            {
                imagesList.Insert(0, request.ImageUrl);
            }

            for (int i = 0; i < imagesList.Count; i++)
            {
                _context.ProductGalleries.Add(new ProductGallery
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Path = imagesList[i],
                    SortOrder = i,
                    IsMain = i == 0
                });
            }
            await _context.SaveChangesAsync();
        }

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
        product.Asin = request.Asin;
        product.ItemModelNumber = request.ItemModelNumber;
        product.Manufacturer = request.Manufacturer;
        product.CountryOfOrigin = request.CountryOfOrigin;
        product.ProductDimensions = request.ProductDimensions;
        product.ItemWeight = request.ItemWeight;
        product.WarrantyInfo = request.WarrantyInfo;
        product.Features = request.Features;
        product.Binding = request.Binding;
        product.ReleaseDate = request.ReleaseDate;
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

        // Handle Images/Gallery Update
        if (!string.IsNullOrEmpty(request.ImageUrl) || (request.Images != null && request.Images.Count > 0))
        {
            var existingGalleries = await _context.ProductGalleries.Where(g => g.ProductId == id).ToListAsync();
            _context.ProductGalleries.RemoveRange(existingGalleries);

            var imagesList = request.Images ?? new List<string>();
            if (!string.IsNullOrEmpty(request.ImageUrl) && !imagesList.Contains(request.ImageUrl))
            {
                imagesList.Insert(0, request.ImageUrl);
            }

            for (int i = 0; i < imagesList.Count; i++)
            {
                _context.ProductGalleries.Add(new ProductGallery
                {
                    Id = Guid.NewGuid(),
                    ProductId = id,
                    Path = imagesList[i],
                    SortOrder = i,
                    IsMain = i == 0
                });
            }
            await _context.SaveChangesAsync();
        }
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
