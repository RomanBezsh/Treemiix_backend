using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateAddressRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string Country,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string City,

    [param: Required]
    [param: StringLength(150, MinimumLength = 2)]
    string Street,

    [param: Required]
    [param: StringLength(20, MinimumLength = 1)]
    string Building,

    [param: StringLength(20)]
    string? Apartment,

    [param: StringLength(20)]
    string? PostalCode,

    bool IsDefault
);

public record UpdateAddressRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string Country,

    [param: Required]
    [param: StringLength(100, MinimumLength = 2)]
    string City,

    [param: Required]
    [param: StringLength(150, MinimumLength = 2)]
    string Street,

    [param: Required]
    [param: StringLength(20, MinimumLength = 1)]
    string Building,

    [param: StringLength(20)]
    string? Apartment,

    [param: StringLength(20)]
    string? PostalCode,

    bool IsDefault
);

public record UpdateProfileRequest(
    DateTime DateOfBirth,

    [StringLength(500)]
    string? AvatarUrl
);

public record UpdateUserRequest(
    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string FirstName,

    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string LastName,

    bool IsActive
);