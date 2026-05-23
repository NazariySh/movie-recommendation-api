namespace MovieRecommendation.Application.DTOs.Search;

public class SearchCountsDto
{
    public int Total => Movies + Series + Artists;

    public int Movies { get; set; }

    public int Series { get; set; }

    public int Artists { get; set; }
}
