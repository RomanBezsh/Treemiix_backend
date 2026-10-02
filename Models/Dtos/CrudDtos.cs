using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateReviewRequest(
    Guid ProductId,

    Guid? ProductGalleryId,

    Guid? ProductVideoId,

    [param: Required]
    [param: StringLength(5000, MinimumLength = 1)]
    string Text,

    [param: Range(1, 5)]
    int Rating,

    List<string>? MediaPaths
);

public record UpdateReviewRequest(
    Guid? ProductGalleryId,

    Guid? ProductVideoId,

    [param: Required]
    [param: StringLength(5000, MinimumLength = 1)]
    string Text,

    [param: Range(1, 5)]
    int Rating
);

public record CreateGalleryRequest(
    Guid ProductId,

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    string Path,

    [Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record UpdateGalleryRequest(
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    string Path,

    [Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record CreateVideoRequest(
    Guid ProductId,

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    string Path,

    [Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record UpdateVideoRequest(
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    string Path,

    [Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record CreateAttributeRequest(
    Guid ProductId,

    [param: Required]
    [param: StringLength(100, MinimumLength = 1)]
    string NameAttr,

    [param: Required]
    [param: StringLength(500, MinimumLength = 1)]
    string Value
);

public record UpdateAttributeRequest(
    [param: Required]
    [param: StringLength(100, MinimumLength = 1)]
    string NameAttr,

    [param: Required]
    [param: StringLength(500, MinimumLength = 1)]
    string Value
);

public record CreateRoleRequest(
    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string Name,

    [param: Range(0, int.MaxValue)]
    int Rights
);

public record UpdateRoleRequest(
    [param: Required]
    [param: StringLength(50, MinimumLength = 2)]
    string Name,

    [param: Range(0, int.MaxValue)]
    int Rights
);

public record CreatePromoCodeProductRequest(
    Guid ProductId,

    Guid PromoCodeId
);