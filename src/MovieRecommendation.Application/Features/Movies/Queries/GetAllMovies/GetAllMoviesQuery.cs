using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetAllMovies;

public record GetAllMoviesQuery(SearchMoviesDto Model, string Lang) : IQuery<PagedList<MovieListItemDto>>;
