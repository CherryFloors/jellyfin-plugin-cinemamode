using System;
using FluentAssertions;
using Jellyfin.Plugin.CinemaMode.Tests.Builders;
using Jellyfin.Plugin.CinemaMode.Tests.Fixtures;
using Xunit;

namespace Jellyfin.Plugin.CinemaMode.Tests
{
    [Collection("Jellyfin")]
    public class SmokeTest
    {
        [Fact]
        public void ConfigBuilder_DefaultValues()
        {
            var config = new ConfigBuilder().Build();
            config.NumberOfTrailers.Should().Be(2);
            config.TrailerPreRollsLibrary.Should().Be("-");
        }

        [Fact]
        public void MovieBuilder_SetsProperties()
        {
            var movie = new MovieBuilder()
                .WithName("Die Hard")
                .WithYear(1988)
                .WithGenres("Action", "Thriller")
                .WithTags("Christmas")
                .WithStudios("20th Century Fox")
                .WithRating(10)
                .Build();

            movie.Name.Should().Be("Die Hard");
            movie.PremiereDate.Should().HaveValue();
            movie.PremiereDate!.Value.Year.Should().Be(1988);
            movie.Genres.Should().BeEquivalentTo("Action", "Thriller");
            movie.Tags.Should().Contain("Christmas");
            movie.Studios.Should().Contain("20th Century Fox");
            movie.InheritedParentalRatingValue.Should().Be(10);
        }

        [Fact]
        public void UserBuilder_CreatesUser()
        {
            var user = new UserBuilder().WithUsername("moviefan").Build();
            user.Username.Should().Be("moviefan");
        }

        [Fact]
        public void SeasonalTagBuilder_SetsProperties()
        {
            var tag = new SeasonalTagBuilder()
                .WithTag("Christmas")
                .WithRange("December 1", "January 5")
                .Build();

            tag.Tag.Should().Be("Christmas");
            tag.Start.Should().Be("December 1");
            tag.End.Should().Be("January 5");
        }

        [Fact]
        public void PreRollSelectionConfigBuilder_DefaultsAllFalse()
        {
            var config = new PreRollSelectionConfigBuilder().Build();
            config.Genre.Should().BeFalse();
            config.Year.Should().BeFalse();
        }

        [Fact]
        public void PreRollSelectionConfigBuilder_All_SetsEverythingTrue()
        {
            var config = new PreRollSelectionConfigBuilder().All().Build();
            config.Genre.Should().BeTrue();
            config.Year.Should().BeTrue();
            config.Decade.Should().BeTrue();
            config.Seasonal.Should().BeTrue();
            config.Studios.Should().BeTrue();
            config.Name.Should().BeTrue();
            config.AllTags.Should().BeTrue();
        }

        [Fact]
        public void MovieBuilder_WithLocalTrailers_SetsExtraIds()
        {
            var movie = new MovieBuilder()
                .WithLocalTrailers(3)
                .Build();

            movie.ExtraIds.Should().HaveCount(3);
        }

        [Fact]
        public void MovieBuilder_CreateTrailerItems_MatchesExtraIds()
        {
            var movie = new MovieBuilder()
                .WithLocalTrailers(2)
                .Build();

            var trailers = MovieBuilder.CreateTrailerItems(movie);
            trailers.Should().HaveCount(2);
            trailers[0].Id.Should().Be(movie.ExtraIds[0]);
            trailers[1].Id.Should().Be(movie.ExtraIds[1]);
        }
    }
}
