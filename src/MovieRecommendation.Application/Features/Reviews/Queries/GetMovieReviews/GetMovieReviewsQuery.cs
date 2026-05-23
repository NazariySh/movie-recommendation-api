using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Reviews.Queries.GetMovieReviews;

public record GetMovieReviewsQuery(Guid MovieId, SearchReviewsDto Query, Guid ViewerId)
    : IQuery<PagedList<MovieReviewDto>>;
