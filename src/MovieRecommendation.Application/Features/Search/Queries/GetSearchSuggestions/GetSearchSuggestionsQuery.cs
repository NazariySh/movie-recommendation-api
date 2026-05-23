using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Search;

namespace MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;

public record GetSearchSuggestionsQuery(string Query, string Lang, int Limit = 5) : IQuery<SearchSuggestionsDto>;
