using AutoMapper;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Domain.Entities.Reviews;

namespace MovieRecommendation.Application.Mapping.Reviews;

public class MovieReviewProfile : Profile
{
    public MovieReviewProfile()
    {
        Guid viewerId = default;

        CreateMap<MovieReview, MovieReviewDto>()
            .ForMember(d => d.AuthorName, opt => opt.MapFrom(s => s.User.UserName ?? string.Empty))
            .ForMember(d => d.AuthorAvatarUrl, opt => opt.MapFrom(s => s.User.AvatarUrl))
            .ForMember(d => d.MarkedHelpful, opt => opt.MapFrom(s =>
                s.HelpfulVotes.Any(v => v.UserId == viewerId)))
            .ForMember(d => d.ReplyCount, opt => opt.MapFrom(s => s.Replies.Count))
            .ForMember(d => d.ReplyToUserName, opt => opt.MapFrom(s =>
                s.ParentReview != null ? s.ParentReview.User.UserName : null));
    }
}
