using System.Net;
using AutoMapper;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Reviews.Commands.CreateReply;

public class CreateReplyCommandHandler : ICommandHandler<CreateReplyCommand, MovieReviewDto>
{
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public CreateReplyCommandHandler(
        IMovieReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cache)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<MovieReviewDto> Handle(CreateReplyCommand request, CancellationToken cancellationToken)
    {
        var parent = await _reviewRepository.GetTrackedAsync(request.ParentReviewId, cancellationToken);

        if (parent is null)
        {
            throw new NotFoundException($"Review {request.ParentReviewId} not found.");
        }

        if (parent.ParentReviewId is not null)
        {
            throw new DomainException(
                HttpStatusCode.BadRequest,
                "Replies are only allowed under a top-level review (depth-1 threading).");
        }

        var reply = new MovieReview
        {
            UserId = request.UserId,
            MovieId = parent.MovieId,
            ParentReviewId = parent.Id,
            Body = request.Model.Body,
            IsSpoiler = request.Model.IsSpoiler,
            Score = null,
        };

        _reviewRepository.Add(reply);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(ReviewCacheKeys.ForMovie(parent.MovieId));

        var replies = await _reviewRepository.GetRepliesAsync(parent.Id, request.UserId, cancellationToken);
        var fresh = replies.FirstOrDefault(r => r.Id == reply.Id);
        return fresh ?? _mapper.Map<MovieReviewDto>(reply);
    }
}
