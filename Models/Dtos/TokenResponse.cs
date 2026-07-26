namespace CloneAmazonBack.Models.Dtos;

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserResponse User
);
