using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;

namespace MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;

public record TriggerRetrainCommand(bool WaitForCompletion = false) : ICommand<RetrainResultDto>;
