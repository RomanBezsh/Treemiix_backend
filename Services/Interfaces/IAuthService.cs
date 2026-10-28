using CloneAmazonBack.DTOs.Auth;

namespace CloneAmazonBack.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<bool> ResendConfirmationAsync(ResendConfirmationRequest request);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(LogoutRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
