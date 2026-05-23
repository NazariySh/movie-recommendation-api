namespace MovieRecommendation.Application.DTOs.Search;

public class SearchSuggestionsDto
{
    public IReadOnlyList<MovieSuggestionDto> Movies { get; set; } = [];

    public IReadOnlyList<ArtistSuggestionDto> Artists { get; set; } = [];
}
