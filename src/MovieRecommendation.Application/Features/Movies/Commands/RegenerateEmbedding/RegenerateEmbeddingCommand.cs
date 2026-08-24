using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Movies.Commands.RegenerateEmbedding;

public record RegenerateEmbeddingCommand(Guid Id) : ICommand;
