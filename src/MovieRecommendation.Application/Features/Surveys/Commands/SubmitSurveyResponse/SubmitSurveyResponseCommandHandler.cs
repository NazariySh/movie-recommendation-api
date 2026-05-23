using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;

public class SubmitSurveyResponseCommandHandler : ICommandHandler<SubmitSurveyResponseCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly ISurveyResponseRepository _surveyRepo;
    private readonly IGenreRepository _genreRepo;
    private readonly IUserGenrePreferenceRepository _preferenceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IOptionsMonitor<SurveySettings> _surveyOptions;

    public SubmitSurveyResponseCommandHandler(
        UserManager<User> userManager,
        ISurveyResponseRepository surveyRepo,
        IGenreRepository genreRepo,
        IUserGenrePreferenceRepository preferenceRepo,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IOptionsMonitor<SurveySettings> surveyOptions)
    {
        _userManager = userManager;
        _surveyRepo = surveyRepo;
        _genreRepo = genreRepo;
        _preferenceRepo = preferenceRepo;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _surveyOptions = surveyOptions;
    }

    public async Task<Unit> Handle(SubmitSurveyResponseCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User with id {request.UserId} not found");

        var schema = _surveyOptions.CurrentValue;
        if (request.Request.Version != schema.Version)
        {
            throw new DomainException(
                System.Net.HttpStatusCode.UnprocessableEntity,
                $"Survey version {request.Request.Version} is no longer accepted; current is {schema.Version}.");
        }

        var answersJson = request.Request.Answers.GetRawText();

        var now = DateTime.UtcNow;

        _surveyRepo.Add(new SurveyResponse
        {
            UserId = user.Id,
            Answers = answersJson,
            Version = schema.Version,
            CompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        });

        var existingGenreIds = await _preferenceRepo.GetGenreIdsAsync(user.Id, cancellationToken);
        if (existingGenreIds.Count == 0)
        {
            var candidateSlugs = SurveyToPreferencesMapper.CandidateGenreSlugs;
            var genres = await _genreRepo.GetBySlugsAsync(candidateSlugs, cancellationToken);
            var preferences = SurveyToPreferencesMapper.Map(user.Id, request.Request.Answers, genres);

            if (preferences.Count > 0)
            {
                await _preferenceRepo.ReplaceForUserAsync(user.Id, preferences, cancellationToken);
            }
        }

        if (!user.OnboardingCompleted)
        {
            user.OnboardingCompleted = true;
            user.UpdatedAt = now;
            await _userManager.UpdateAsync(user);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id));

        return Unit.Value;
    }
}
