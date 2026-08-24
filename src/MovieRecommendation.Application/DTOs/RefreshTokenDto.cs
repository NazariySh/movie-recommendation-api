namespace MovieRecommendation.Application.DTOs;

public record RefreshTokenDto(string Token, DateTime ExpiryTime);
