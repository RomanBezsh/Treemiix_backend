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
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation(
            "Начата регистрация пользователя с email {Email}",
            request.Email);

        var userExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (userExists)
        {
            _logger.LogWarning(
                "Попытка регистрации с уже существующим email {Email}",
                request.Email);

            throw new InvalidOperationException(
                "User with this email already exists");
        }

        var role = await _context.UserRoles
            .FirstOrDefaultAsync(r => r.Name == "User");

        if (role is null)
        {
            _logger.LogInformation(
                "Роль User не найдена. Создаётся новая роль");

            role = new UserRole
            {
                Id = Guid.NewGuid(),
                Name = "User",
                Rights = 1
            };

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

        _logger.LogInformation(
            "Пользователь {Email} успешно зарегистрирован. Id: {UserId}",
            user.Email,
            user.Id);

        return GenerateAuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            role.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation(
            "Попытка входа пользователя {Email}",
            request.Email);

        var user = await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            _logger.LogWarning(
                "Пользователь с email {Email} не найден",
                request.Email);

            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.Password);

        if (!passwordIsValid)
        {
            _logger.LogWarning(
                "Введён неверный пароль для пользователя {Email}",
                request.Email);

            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Попытка входа в отключённый аккаунт {Email}",
                request.Email);

            throw new UnauthorizedAccessException(
                "Account is disabled");
        }

        if (user.UserRole is null)
        {
            _logger.LogError(
                "У пользователя {Email} не найдена роль",
                request.Email);

            throw new InvalidOperationException(
                "User role was not found");
        }

        _logger.LogInformation(
            "Пользователь {Email} успешно вошёл в систему",
            user.Email);

        return GenerateAuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.UserRole.Name);
    }

    private AuthResponse GenerateAuthResponse(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        string roleName)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var jwtKey = jwtSettings["Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            _logger.LogError(
                "JWT ключ не найден в конфигурации");

            throw new InvalidOperationException(
                "JWT key is not configured");
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                ClaimTypes.Email,
                email),

            new Claim(
                ClaimTypes.GivenName,
                firstName),

            new Claim(
                ClaimTypes.Surname,
                lastName),

            new Claim(
                ClaimTypes.Role,
                roleName)
        };

        var expiryHours = double.TryParse(
            jwtSettings["ExpiryHours"],
            out var hours)
            ? hours
            : 24;

        var expiry = DateTime.UtcNow.AddHours(expiryHours);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        _logger.LogInformation(
            "JWT-токен успешно создан для пользователя {Email}",
            email);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler()
                .WriteToken(token),

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