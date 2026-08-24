using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is invalid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must contain between 6 and 100 characters.")]
    public string Password { get; set; } = string.Empty;
}