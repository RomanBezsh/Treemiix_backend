namespace CloneAmazonBack.Models.Dtos;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid UserRoleId
);

public record LoginRequest(
    string Email,
    string Password
);

public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string RoleName
);
