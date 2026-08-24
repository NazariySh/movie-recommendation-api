namespace MovieRecommendation.Application.DTOs.Users;

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}
