namespace MovieRecommendation.Application.Models;

public class GooglePayload
{
    public string Email { get; set; } = null!;

    public string? GivenName { get; set; }

    public string? FamilyName { get; set; }

    public string? Picture { get; set; }

    public bool EmailVerified { get; set; }

    public string Subject { get; set; } = null!;
}
