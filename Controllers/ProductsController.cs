using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ISellerService _sellerService;
    private readonly ICategoryService _categoryService;

    public ProductsController(
        IProductService productService,
        ISellerService sellerService,
        ICategoryService categoryService)
    {
        _productService = productService;
        _sellerService = sellerService;
        _categoryService = categoryService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId, [FromQuery] Guid? sellerId, [FromQuery] bool? isActive)
    {
        var products = await _productService.GetAllAsync(categoryId, sellerId, isActive);
        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null) return NotFound();
        return Ok(product);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            var seller = await GetOwnSellerAsync();
            if (seller is null)
                return Forbid();

            // A seller can only publish products under their own store.
            request = request with { SellerId = seller.Id };
        }

        if (await _sellerService.GetByIdAsync(request.SellerId) is null)
            return BadRequest(new { message = "Seller not found" });

        if (await _categoryService.GetByIdAsync(request.CategoryId) is null)
            return BadRequest(new { message = "Category not found" });

        try
        {
            var product = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateProductRequest request)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        if (!User.IsInRole(Roles.Admin))
        {
            var seller = await GetOwnSellerAsync();
            if (seller is null || product.SellerId != seller.Id)
                return Forbid();

            request = request with { SellerId = seller.Id };
        }

        if (await _categoryService.GetByIdAsync(request.CategoryId) is null)
            return BadRequest(new { message = "Category not found" });

        await _productService.UpdateAsync(id, request);
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        if (!User.IsInRole(Roles.Admin))
        {
            var seller = await GetOwnSellerAsync();
            if (seller is null || product.SellerId != seller.Id)
                return Forbid();
        }

        await _productService.SoftDeleteAsync(id);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        await _productService.HardDeleteAsync(id);
        return NoContent();
    }

    private async Task<Seller?> GetOwnSellerAsync()
    {
        var seller = await _sellerService.GetByUserAsync(User.GetUserId());
        return seller is { Status: SellerStatus.Active } ? seller : null;
    }
}