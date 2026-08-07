using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateAddressRequest(
    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string Country,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string City,

    [property: Required]
    [property: StringLength(150, MinimumLength = 2)]
    string Street,

    [property: Required]
    [property: StringLength(20, MinimumLength = 1)]
    string Building,

    [property: StringLength(20)]
    string? Apartment,

    [property: StringLength(20)]
    string? PostalCode,

    bool IsDefault
);

public record UpdateAddressRequest(
    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string Country,

    [property: Required]
    [property: StringLength(100, MinimumLength = 2)]
    string City,

    [property: Required]
    [property: StringLength(150, MinimumLength = 2)]
    string Street,

    [property: Required]
    [property: StringLength(20, MinimumLength = 1)]
    string Building,

    [property: StringLength(20)]
    string? Apartment,

    [property: StringLength(20)]
    string? PostalCode,

    bool IsDefault
);

public record UpdateProfileRequest(
    DateTime DateOfBirth,

    [property: StringLength(500)]
    string? AvatarUrl
);

public record UpdateUserRequest(
    [property: Required]
    [property: StringLength(50, MinimumLength = 2)]
    string FirstName,

    [property: Required]
    [property: StringLength(50, MinimumLength = 2)]
    string LastName,

    bool IsActive
);