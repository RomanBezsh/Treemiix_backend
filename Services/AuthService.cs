using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private const int ConfirmationCodeLifetimeMinutes = 15;
    private const int PasswordResetCodeLifetimeMinutes = 15;

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailSender _emailSender;

    public AuthService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailSender? emailSender = null)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailSender = emailSender ?? NullEmailSender.Instance;
    }

    private bool RequireEmailConfirmation =>
        _configuration.GetValue("Auth:RequireEmailConfirmation", false);

    private int MaxFailedLoginAttempts =>
        _configuration.GetValue("Auth:MaxFailedLoginAttempts", 5);

    private int LockoutMinutes =>
        _configuration.GetValue("Auth:LockoutMinutes", 15);

    private int RefreshTokenDays =>
        _configuration.GetValue("Jwt:RefreshTokenDays", 30);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("User registration started for email {Email}", request.Email);

        var userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (userExists)
        {
            _logger.LogWarning("Registration attempt with an existing email {Email}", request.Email);
            throw new InvalidOperationException("User with this email already exists");
        }

        var role = await _context.UserRoles.FirstOrDefaultAsync(r => r.Name == "User");
        if (role is null)
        {
            _logger.LogInformation("User role was not found. A new role is being created");

            role = new UserRole
            {
                Id = Guid.NewGuid(),
                Name = "User",
                Rights = 1
            };

            _context.UserRoles.Add(role);
        }

        var requireEmailConfirmation = RequireEmailConfirmation;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            EmailConfirmed = !requireEmailConfirmation,
            EmailConfirmationCode = GenerateNumericCode(),
            EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(ConfirmationCodeLifetimeMinutes),
            UserRoleId = role.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Email} was registered successfully. Id: {UserId}",
            user.Email,
            user.Id);

        if (requireEmailConfirmation && !user.EmailConfirmed)
        {
            await TrySendConfirmationEmailAsync(user);

            return EmailConfirmationResponse(user, role.Name);
        }

        return await IssueTokensAsync(user, role.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Email}", request.Email);

        var user = await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            _logger.LogWarning("User with email {Email} was not found", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account {Email}", request.Email);
            throw new UnauthorizedAccessException(
                "Account is temporarily locked. Please try again later.");
        }

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

        if (!passwordIsValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;
                _logger.LogWarning("Account {Email} locked after too many failed logins", request.Email);
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Invalid password entered for user {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for disabled account {Email}", request.Email);
            throw new UnauthorizedAccessException("Account is disabled");
        }

        if (RequireEmailConfirmation && !user.EmailConfirmed)
        {
            _logger.LogWarning("Login attempt for unconfirmed email {Email}", request.Email);
            throw new UnauthorizedAccessException(
                "Email is not confirmed. Please confirm your email first.");
        }

        if (user.UserRole is null)
        {
            _logger.LogError("Role was not found for user {Email}", request.Email);
            throw new InvalidOperationException("User role was not found");
        }

        if (user.FailedLoginAttempts != 0 || user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return await IssueTokensAsync(user, user.UserRole.Name);
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        _logger.LogInformation("Email confirmation attempt for {Email}", request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            _logger.LogWarning("Confirmation attempt for unknown email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid or expired confirmation code");
        }

        if (user.EmailConfirmed)
            return true;

        if (string.IsNullOrEmpty(user.EmailConfirmationCode) ||
            user.EmailConfirmationCodeExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Confirmation code expired for {Email}", request.Email);
            throw new InvalidOperationException(
                "Confirmation code is expired. Please request a new one.");
        }

        if (user.EmailConfirmationCode != request.Code)
        {
            _logger.LogWarning("Invalid confirmation code for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid or expired confirmation code");
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiresAt = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email {Email} confirmed successfully", request.Email);
        return true;
    }

    public async Task<bool> ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Always report success to avoid account enumeration.
        if (user is null || user.EmailConfirmed)
            return true;

        user.EmailConfirmationCode = GenerateNumericCode();
        user.EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(ConfirmationCodeLifetimeMinutes);
        await _context.SaveChangesAsync();

        await TrySendConfirmationEmailAsync(user);
        return true;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
    {
        var hash = HashToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.UserRole)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Reuse of an already rotated token means the token may be stolen.
        if (stored.RevokedAt.HasValue)
        {
            await RevokeAllRefreshTokensAsync(stored.UserId);
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired");

        if (!stored.User.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        if (stored.User.UserRole is null)
            throw new InvalidOperationException("User role was not found");

        var (rawToken, entity) = CreateRefreshToken(stored.UserId);

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenHash = entity.TokenHash;
        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync();

        var (accessToken, expiresAt) = CreateAccessToken(
            stored.User.Id,
            stored.User.Email,
            stored.User.FirstName,
            stored.User.LastName,
            stored.User.UserRole.Name);

        return new AuthResponse
        {
            Token = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = rawToken,
            RefreshTokenExpiresAt = entity.ExpiresAt,
            User = BuildUserInfo(stored.User, stored.User.UserRole.Name)
        };
    }

    public async Task<bool> LogoutAsync(LogoutRequest request)
    {
        var hash = HashToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || stored.RevokedAt.HasValue)
            return false;

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new UnauthorizedAccessException("User was not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await RevokeAllRefreshTokensAsync(userId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {Email}", user.Email);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Always report success to avoid account enumeration.
        if (user is null || !user.IsActive)
            return;

        user.PasswordResetCode = GenerateNumericCode();
        user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetCodeLifetimeMinutes);
        await _context.SaveChangesAsync();

        await TrySendPasswordResetEmailAsync(user);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null ||
            string.IsNullOrEmpty(user.PasswordResetCode) ||
            user.PasswordResetCodeExpiresAt < DateTime.UtcNow ||
            user.PasswordResetCode != request.Code)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset code");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAt = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await RevokeAllRefreshTokensAsync(user.Id);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password was reset for user {Email}", user.Email);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string roleName)
    {
        var (rawToken, entity) = CreateRefreshToken(user.Id);
        _context.RefreshTokens.Add(entity);

        // Keep the table small: drop expired tokens for this user.
        var expired = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count > 0)
            _context.RefreshTokens.RemoveRange(expired);

        await _context.SaveChangesAsync();

        var (accessToken, expiresAt) = CreateAccessToken(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            roleName);

        return new AuthResponse
        {
            Token = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = rawToken,
            RefreshTokenExpiresAt = entity.ExpiresAt,
            User = BuildUserInfo(user, roleName)
        };
    }

    private (string Token, DateTime ExpiresAt) CreateAccessToken(
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
            _logger.LogError("JWT key was not found in the configuration");
            throw new InvalidOperationException("JWT key is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.GivenName, firstName),
            new Claim(ClaimTypes.Surname, lastName),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiryHours = double.TryParse(
            jwtSettings["ExpiryHours"],
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
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

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = DateTime.UtcNow;

        return (rawToken, new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(RefreshTokenDays)
        });
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    private static string GenerateNumericCode() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);

    private static AuthResponse EmailConfirmationResponse(User user, string roleName) => new()
    {
        Token = string.Empty,
        ExpiresAt = default,
        RefreshToken = string.Empty,
        RefreshTokenExpiresAt = default,
        EmailConfirmationRequired = true,
        User = BuildUserInfo(user, roleName)
    };

    private static UserInfo BuildUserInfo(User user, string roleName) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = roleName
    };

    private async Task TrySendConfirmationEmailAsync(User user)
    {
        var code = user.EmailConfirmationCode;
        if (string.IsNullOrEmpty(code))
            return;

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Подтверждение email — Treemiix",
                $"""
                 <p>Здравствуйте, {WebUtility.HtmlEncode(user.FirstName)}!</p>
                 <p>Ваш код подтверждения: <b style="font-size:20px">{code}</b></p>
                 <p>Код действует {ConfirmationCodeLifetimeMinutes} минут.</p>
                 """);
        }
        catch (Exception ex)
        {
            // Registration must not fail because the mail provider is temporarily down;
            // the user can request a new code through /api/auth/resend-confirmation.
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }
    }

    private async Task TrySendPasswordResetEmailAsync(User user)
    {
        var code = user.PasswordResetCode;
        if (string.IsNullOrEmpty(code))
            return;

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Сброс пароля — Treemiix",
                $"""
                 <p>Здравствуйте, {WebUtility.HtmlEncode(user.FirstName)}!</p>
                 <p>Код для сброса пароля: <b style="font-size:20px">{code}</b></p>
                 <p>Код действует {PasswordResetCodeLifetimeMinutes} минут. Если вы не запрашивали сброс, просто игнорируйте письмо.</p>
                 """);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }
}
