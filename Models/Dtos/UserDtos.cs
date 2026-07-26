namespace CloneAmazonBack.Models.Dtos;

public record CreateAddressRequest(
    Guid UserId,
    string Country,
    string City,
    string Street,
    string Building,
    string? Apartment,
    string? PostalCode,
    bool IsDefault
);

public record UpdateAddressRequest(
    string Country,
    string City,
    string Street,
    string Building,
    string? Apartment,
    string? PostalCode,
    bool IsDefault
);

public record UpdateProfileRequest(
    DateTime DateOfBirth,
    string? AvatarUrl
);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    bool IsActive
);
