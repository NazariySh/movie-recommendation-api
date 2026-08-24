using FluentAssertions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Mapping;

public class AutoMapperProfilesTests
{
    [Fact]
    public void AssertConfigurationIsValid_Should_Pass_When_AllProfilesLoadedFromApplicationAssembly()
    {
        var configuration = TestMapperFactory.CreateConfiguration();

        Action act = configuration.AssertConfigurationIsValid;

        act.Should().NotThrow(
            "every CreateMap in Application must resolve every destination member " +
            "or explicitly Ignore/MapFrom it");
    }

    [Fact]
    public void CreateMapper_Should_ReturnUsableMapper_When_ConfigurationIsValid()
    {
        var mapper = TestMapperFactory.Create();

        mapper.Should().NotBeNull();
        mapper.ConfigurationProvider.Should().NotBeNull();
    }
}
