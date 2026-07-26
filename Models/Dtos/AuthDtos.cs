namespace CloneAmazonBack.Models.Dtos;

public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string RoleName
);
