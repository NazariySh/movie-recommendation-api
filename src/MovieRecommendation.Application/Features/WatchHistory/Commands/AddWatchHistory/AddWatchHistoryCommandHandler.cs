using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.WatchHistory.Commands.AddWatchHistory;

public class AddWatchHistoryCommandHandler : ICommandHandler<AddWatchHistoryCommand>
{
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddWatchHistoryCommandHandler(
        IWatchHistoryRepository watchHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _watchHistoryRepository = watchHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AddWatchHistoryCommand request, CancellationToken cancellationToken)
    {
        await _watchHistoryRepository.UpsertAsync(
            request.UserId,
            request.Model.MovieId,
            request.Model.WatchedAt ?? DateTime.UtcNow,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
