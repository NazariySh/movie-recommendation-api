using MediatR;

namespace MovieRecommendation.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Unit>, IBaseCommand;

public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand;

public interface IBaseCommand;
