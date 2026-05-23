using MediatR;

namespace MovieRecommendation.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
