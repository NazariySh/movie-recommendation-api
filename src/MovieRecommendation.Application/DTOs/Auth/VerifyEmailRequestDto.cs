namespace MovieRecommendation.Application.DTOs.Auth;

public class VerifyEmailRequestDto
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;
}
