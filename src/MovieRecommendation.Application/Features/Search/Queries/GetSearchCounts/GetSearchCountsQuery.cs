using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Search;

namespace MovieRecommendation.Application.Features.Search.Queries.GetSearchCounts;

public record GetSearchCountsQuery(string Query, string Lang) : IQuery<SearchCountsDto>;
