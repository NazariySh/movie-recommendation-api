namespace MovieRecommendation.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}
