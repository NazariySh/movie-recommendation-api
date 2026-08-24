using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MovieRecommendation.Application.Mapping.Movies;

namespace MovieRecommendation.UnitTests.Infrastructure;

public static class TestMapperFactory
{
    public static MapperConfiguration CreateConfiguration()
    {
        var expression = new MapperConfigurationExpression();
        expression.AddMaps(typeof(MovieProfile).Assembly);

        return new MapperConfiguration(expression, NullLoggerFactory.Instance);
    }

    public static IMapper Create()
    {
        return CreateConfiguration().CreateMapper();
    }
}
