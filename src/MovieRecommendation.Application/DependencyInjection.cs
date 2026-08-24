using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MovieRecommendation.Application.Abstractions.Behaviors;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Mapping.Movies;

namespace MovieRecommendation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddAutoMapper(config => config.AddMaps(typeof(MovieProfile).Assembly));

        services.AddScoped<IArtistSlugGenerator, ArtistSlugGenerator>();
        services.AddScoped<IMovieKeyGenerator, MovieKeyGenerator>();

        return services;
    }
}
