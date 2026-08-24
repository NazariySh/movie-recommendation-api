namespace MovieRecommendation.Application.DTOs.Auth;

public record LoginResponseDto(string AccessToken, AuthUserDto User);
