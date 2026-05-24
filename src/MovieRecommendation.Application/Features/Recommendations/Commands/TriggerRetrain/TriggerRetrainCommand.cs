using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;

namespace MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;

public record TriggerRetrainCommand(Guid ActorUserId, bool WaitForCompletion = false) : ICommand<RetrainResultDto>;
