using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.DTOs.Auth;

public class RegisterRequest
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

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(
        50,
        MinimumLength = 2,
        ErrorMessage = "First name must contain between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(
        50,
        MinimumLength = 2,
        ErrorMessage = "Last name must contain between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;
}