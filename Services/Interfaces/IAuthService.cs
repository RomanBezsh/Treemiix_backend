using CloneAmazonBack.DTOs.Auth;

namespace CloneAmazonBack.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
}
