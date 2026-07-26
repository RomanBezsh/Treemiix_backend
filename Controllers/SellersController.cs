using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SellersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SellersController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sellers = await _context.Sellers.ToListAsync();
        return Ok(sellers);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller == null) return NotFound();
        return Ok(seller);
    }

    [AllowAnonymous]
    [HttpGet("byuser/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        if (seller == null) return NotFound();
        return Ok(seller);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSellerRequest request)
    {
        var userId = User.GetUserId();

        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoreName = request.StoreName,
            StoreSlug = request.StoreSlug,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            TaxNumber = request.TaxNumber,
            LegalAddress = request.LegalAddress,
            BankAccount = request.BankAccount,
            CommissionRate = request.CommissionRate,
            Status = SellerStatus.PendingVerification,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = seller.Id }, seller);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateSellerRequest request)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller == null) return NotFound();

        seller.StoreName = request.StoreName;
        seller.StoreSlug = request.StoreSlug;
        seller.LogoUrl = request.LogoUrl;
        seller.Description = request.Description;
        seller.TaxNumber = request.TaxNumber;
        seller.LegalAddress = request.LegalAddress;
        seller.BankAccount = request.BankAccount;
        seller.CommissionRate = request.CommissionRate;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, SellerStatus status)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller == null) return NotFound();

        seller.Status = status;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
