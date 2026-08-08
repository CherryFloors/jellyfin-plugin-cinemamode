using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.CinemaMode.Configuration;
using Jellyfin.Plugin.CinemaMode.Tests.Builders;
using Jellyfin.Plugin.CinemaMode.Tests.Fixtures;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.CinemaMode.Tests
{
    [Collection("Jellyfin")]
    public class TrailerSelectorTests
    {
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly ILogger _logger;

        public TrailerSelectorTests(JellyfinFixture fixture)
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _logger = NullLogger.Instance;

            // Use the same mock for both injected and static BaseItem.LibraryManager
            // so that Movie.LocalTrailers (which calls the static) works
            BaseItem.LibraryManager = _mockLibraryManager.Object;
        }

        private Movie CreateMovieWithTrailers(string name, int trailerCount, int? rating = null)
        {
            var movie = new MovieBuilder()
                .WithName(name)
                .WithPath($"/movies/{name}/{name}.mkv")
                .WithLocalTrailers(trailerCount)
                .WithRating(rating ?? 10)
                .Build();

            var trailers = MovieBuilder.CreateTrailerItems(movie);
            foreach (var trailer in trailers)
            {
                _mockLibraryManager
                    .Setup(m => m.GetItemById(trailer.Id))
                    .Returns(trailer);
            }

            return movie;
        }

        private TrailerSelector CreateSelector(
            Movie feature,
            PluginConfiguration? config = null,
            TimeProvider? timeProvider = null)
        {
            var cfg = config ?? new ConfigBuilder()
                .WithNumberOfTrailers(2)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var user = new UserBuilder().Build();

            return new TrailerSelector(
                feature, user, cfg,
                _mockLibraryManager.Object,
                timeProvider ?? TimeProvider.System,
                _logger);
        }

        // ── GetTrailers: Rating Enforcement ──

        [Fact]
        public void GetTrailers_EnforceRating_UnratedFeature_ReturnsEmpty()
        {
            var feature = new MovieBuilder().Build(); // no InheritedParentalRatingValue

            var config = new ConfigBuilder()
                .WithNumberOfTrailers(2)
                .WithEnforceRatingLimitTrailers(true)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().BeEmpty();
        }

        // ── GetTrailers: Trailer Count Limit ──

        [Fact]
        public void GetTrailers_RespectsNumberOfTrailersLimit()
        {
            var movie1 = CreateMovieWithTrailers("Movie1", 1);
            var movie2 = CreateMovieWithTrailers("Movie2", 1);
            var movie3 = CreateMovieWithTrailers("Movie3", 1);

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie1, movie2, movie3 }.AsReadOnly());

            var feature = new MovieBuilder().WithRating(10).Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(2)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().HaveCount(2);
        }

        [Fact]
        public void GetTrailers_EmptyLibrary_ReturnsEmpty()
        {
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem>().AsReadOnly());

            var feature = new MovieBuilder().Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(2)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().BeEmpty();
        }

        // ── GetTrailers: Fallback Chain ──

        [Fact]
        public void GetTrailers_SelectionRulesExhausted_FallsToUnplayed()
        {
            var unplayedMovie = CreateMovieWithTrailers("Unplayed", 1);

            var callCount = 0;
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(() =>
                {
                    callCount++;
                    // First call is from selection rules - return empty
                    if (callCount == 1) return new List<BaseItem>().AsReadOnly();
                    // Second call is unplayed fallback - return movie
                    return new List<BaseItem> { unplayedMovie }.AsReadOnly();
                });

            var selectionRule = new TrailerSelectionConfigBuilder().WithGenre().Build();
            var feature = new MovieBuilder().WithGenres("Action").Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .WithTrailerSelectionRules(selectionRule)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().ContainSingle();
        }

        [Fact]
        public void GetTrailers_ReturnsIntroInfoWithPathAndId()
        {
            var movie = CreateMovieWithTrailers("Movie", 1);
            var expectedTrailer = MovieBuilder.CreateTrailerItems(movie).First();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie }.AsReadOnly());

            var feature = new MovieBuilder().Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().ContainSingle();
            trailers[0].ItemId.Should().Be(expectedTrailer.Id);
            trailers[0].Path.Should().Be(expectedTrailer.Path);
        }

        [Fact]
        public void GetTrailers_ExcludesFeatureFromResults()
        {
            var featureId = Guid.NewGuid();
            var feature = new MovieBuilder().WithId(featureId).Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem>().AsReadOnly());

            var config = new ConfigBuilder()
                .WithNumberOfTrailers(2)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var selector = CreateSelector(feature, config);
            selector.GetTrailers().ToList();

            _mockLibraryManager.Verify(m => m.GetItemList(
                It.Is<InternalItemsQuery>(q => q.ExcludeItemIds.Contains(featureId))),
                Times.AtLeastOnce);
        }

        [Fact]
        public void GetTrailers_MoviesWithoutLocalTrailers_AreFiltered()
        {
            var movieNoTrailers = new MovieBuilder()
                .WithName("NoTrailers")
                .WithRating(10)
                .Build();

            var movieWithTrailers = CreateMovieWithTrailers("WithTrailers", 1);

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movieNoTrailers, movieWithTrailers }.AsReadOnly());

            var feature = new MovieBuilder().Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var selector = CreateSelector(feature, config);

            var trailers = selector.GetTrailers().ToList();

            trailers.Should().ContainSingle();
        }

        [Fact]
        public void GetTrailers_SelectionRuleWithYear_FiltersToFeatureYear()
        {
            var movie = CreateMovieWithTrailers("80sMovie", 1);

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie }.AsReadOnly());

            var selectionRule = new TrailerSelectionConfigBuilder().WithYear().Build();
            var feature = new MovieBuilder().WithYear(1988).Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .WithTrailerSelectionRules(selectionRule)
                .Build();

            var selector = CreateSelector(feature, config);
            selector.GetTrailers().ToList();

            _mockLibraryManager.Verify(m => m.GetItemList(
                It.Is<InternalItemsQuery>(q =>
                    q.MinPremiereDate == new DateTime(1988, 1, 1) &&
                    q.MaxPremiereDate == new DateTime(1988, 12, 31))),
                Times.AtLeastOnce);
        }

        [Fact]
        public void GetTrailers_SelectionRuleWithDecade_FiltersToDecadeRange()
        {
            var movie = CreateMovieWithTrailers("80sMovie", 1);

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie }.AsReadOnly());

            var selectionRule = new TrailerSelectionConfigBuilder().WithDecade().Build();
            var feature = new MovieBuilder().WithYear(1985).Build();
            var config = new ConfigBuilder()
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .WithTrailerSelectionRules(selectionRule)
                .Build();

            var selector = CreateSelector(feature, config);
            selector.GetTrailers().ToList();

            _mockLibraryManager.Verify(m => m.GetItemList(
                It.Is<InternalItemsQuery>(q =>
                    q.MinPremiereDate == new DateTime(1980, 1, 1) &&
                    q.MaxPremiereDate == new DateTime(1989, 12, 31))),
                Times.AtLeastOnce);
        }
    }
}
