using System.ComponentModel.DataAnnotations;

namespace CloneAmazonBack.Models.Dtos;

public record CreateReviewRequest(
    Guid ProductId,

    Guid? ProductGalleryId,

    Guid? ProductVideoId,

    [property: Required]
    [property: StringLength(5000, MinimumLength = 1)]
    string Text,

    [property: Range(1, 5)]
    int Rating
);

public record UpdateReviewRequest(
    Guid? ProductGalleryId,

    Guid? ProductVideoId,

    [property: Required]
    [property: StringLength(5000, MinimumLength = 1)]
    string Text,

    [property: Range(1, 5)]
    int Rating
);

public record CreateGalleryRequest(
    Guid ProductId,

    [property: Required]
    [property: StringLength(1000, MinimumLength = 1)]
    string Path,

    [property: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record UpdateGalleryRequest(
    [property: Required]
    [property: StringLength(1000, MinimumLength = 1)]
    string Path,

    [property: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record CreateVideoRequest(
    Guid ProductId,

    [property: Required]
    [property: StringLength(1000, MinimumLength = 1)]
    string Path,

    [property: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record UpdateVideoRequest(
    [property: Required]
    [property: StringLength(1000, MinimumLength = 1)]
    string Path,

    [property: Range(0, int.MaxValue)]
    int SortOrder,

    bool IsMain
);

public record CreateAttributeRequest(
    Guid ProductId,

    [property: Required]
    [property: StringLength(100, MinimumLength = 1)]
    string NameAttr,

    [property: Required]
    [property: StringLength(500, MinimumLength = 1)]
    string Value
);

public record UpdateAttributeRequest(
    [property: Required]
    [property: StringLength(100, MinimumLength = 1)]
    string NameAttr,

    [property: Required]
    [property: StringLength(500, MinimumLength = 1)]
    string Value
);

public record CreateRoleRequest(
    [property: Required]
    [property: StringLength(50, MinimumLength = 2)]
    string Name,

    [property: Range(0, int.MaxValue)]
    int Rights
);

public record UpdateRoleRequest(
    [property: Required]
    [property: StringLength(50, MinimumLength = 2)]
    string Name,

    [property: Range(0, int.MaxValue)]
    int Rights
);

public record CreatePromoCodeProductRequest(
    Guid ProductId,

    Guid PromoCodeId
);