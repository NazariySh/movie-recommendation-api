using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Genres;

namespace MovieRecommendation.Application.Features.Genres.Queries.GetAllGenres;

public record GetAllGenresQuery(string Lang) : IQuery<IReadOnlyList<GenreDto>>;
