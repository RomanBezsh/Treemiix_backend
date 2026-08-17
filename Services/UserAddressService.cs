using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class UserAddressService : IUserAddressService
{
    private readonly AppDbContext _context;

    public UserAddressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAddress>> GetByUserAsync(Guid userId)
    {
        return await _context.UserAddresses
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public async Task<UserAddress> CreateAsync(Guid userId, CreateAddressRequest request)
    {
        if (request.IsDefault)
        {
            var currentDefault = await _context.UserAddresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .FirstOrDefaultAsync();

            if (currentDefault != null)
                currentDefault.IsDefault = false;
        }

        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Country = request.Country,
            City = request.City,
            Street = request.Street,
            Building = request.Building,
            Apartment = request.Apartment,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };

        _context.UserAddresses.Add(address);
        await _context.SaveChangesAsync();

        return address;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateAddressRequest request, Guid userId)
    {
        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null)
            return false;

        if (request.IsDefault && !address.IsDefault)
        {
            var currentDefault = await _context.UserAddresses
                .Where(a => a.UserId == address.UserId && a.IsDefault && a.Id != id)
                .FirstOrDefaultAsync();

            if (currentDefault != null)
                currentDefault.IsDefault = false;
        }

        address.Country = request.Country;
        address.City = request.City;
        address.Street = request.Street;
        address.Building = request.Building;
        address.Apartment = request.Apartment;
        address.PostalCode = request.PostalCode;
        address.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null)
            return false;

        _context.UserAddresses.Remove(address);
        await _context.SaveChangesAsync();

        return true;
    }
}