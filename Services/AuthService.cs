using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloneAmazonBack.Data;
using CloneAmazonBack.DTOs.Auth;
using CloneAmazonBack.Models;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CloneAmazonBack.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("User with this email already exists");

        var role = await _context.UserRoles.FirstOrDefaultAsync(r => r.Name == "User");
        if (role is null)
        {
            role = new UserRole { Id = Guid.NewGuid(), Name = "User", Rights = 1 };
            _context.UserRoles.Add(role);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            UserRoleId = role.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateAuthResponse(user.Id, user.Email, user.FirstName, user.LastName, role.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        return GenerateAuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.UserRole.Name);
    }

    private AuthResponse GenerateAuthResponse(Guid userId, string email, string firstName, string lastName, string roleName)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.GivenName, firstName),
            new Claim(ClaimTypes.Surname, lastName),
            new Claim(ClaimTypes.Role, roleName)
        };

        var expiry = DateTime.UtcNow.AddHours(
            double.Parse(jwtSettings["ExpiryHours"] ?? "24"));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiry,
            User = new UserInfo
            {
                Id = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = roleName
            }
        };
    }
}
