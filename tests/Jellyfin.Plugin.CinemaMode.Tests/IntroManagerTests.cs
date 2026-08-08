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
    public class IntroManagerTests
    {
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly ILogger _logger;

        public IntroManagerTests(JellyfinFixture fixture)
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _logger = NullLogger.Instance;
            BaseItem.LibraryManager = _mockLibraryManager.Object;
        }

        private Movie CreateMovieWithTrailers(string name, int trailerCount)
        {
            var movie = new MovieBuilder()
                .WithName(name)
                .WithPath($"/movies/{name}/{name}.mkv")
                .WithLocalTrailers(trailerCount)
                .WithRating(10)
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

        [Fact]
        public void Get_TrailerPreRollDisabled_SkipsTrailerPreRoll()
        {
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary("-")
                .WithFeaturePreRollLibrary("-")
                .WithNumberOfTrailers(0)
                .Build();

            var manager = new IntroManager(_mockLibraryManager.Object, config, _logger);
            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();

            var results = manager.Get(feature, user).ToList();

            results.Should().BeEmpty();
            _mockLibraryManager.Verify(
                m => m.GetItemList(It.IsAny<InternalItemsQuery>()),
                Times.Never);
        }

        [Fact]
        public void Get_ZeroTrailers_SkipsTrailerSelection()
        {
            var preRoll = new MovieBuilder()
                .WithPath("/prerolls/trailer.mkv")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { preRoll }.AsReadOnly());

            var libraryId = Guid.NewGuid().ToString();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(libraryId)
                .WithFeaturePreRollLibrary("-")
                .WithTrailerPreRollsRatingLimit(false)
                .WithNumberOfTrailers(0)
                .Build();

            var manager = new IntroManager(_mockLibraryManager.Object, config, _logger);
            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();

            var results = manager.Get(feature, user).ToList();

            // Should have trailer pre-roll but no trailers
            results.Should().ContainSingle();
            results[0].Path.Should().Be("/prerolls/trailer.mkv");
        }

        [Fact]
        public void Get_AllEnabled_ReturnsCorrectOrdering()
        {
            var trailerPreRoll = new MovieBuilder()
                .WithName("TrailerPreRoll")
                .WithPath("/prerolls/trailer-preroll.mkv")
                .Build();
            var featurePreRoll = new MovieBuilder()
                .WithName("FeaturePreRoll")
                .WithPath("/prerolls/feature-preroll.mkv")
                .Build();
            var trailerMovie = CreateMovieWithTrailers("TrailerMovie", 1);

            var trailerPreRollLibrary = Guid.NewGuid().ToString();
            var featurePreRollLibrary = Guid.NewGuid().ToString();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                    q => q.ParentId == Guid.Parse(trailerPreRollLibrary))))
                .Returns(new List<BaseItem> { trailerPreRoll }.AsReadOnly());

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                    q => q.ParentId == Guid.Parse(featurePreRollLibrary))))
                .Returns(new List<BaseItem> { featurePreRoll }.AsReadOnly());

            // Trailer queries don't have a ParentId
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                    q => q.HasTrailer == true)))
                .Returns(new List<BaseItem> { trailerMovie }.AsReadOnly());

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(trailerPreRollLibrary)
                .WithFeaturePreRollLibrary(featurePreRollLibrary)
                .WithTrailerPreRollsRatingLimit(false)
                .WithFeaturePreRollsRatingLimit(false)
                .WithNumberOfTrailers(1)
                .WithEnforceRatingLimitTrailers(false)
                .Build();

            var manager = new IntroManager(_mockLibraryManager.Object, config, _logger);
            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();

            var results = manager.Get(feature, user).ToList();

            // Order should be: trailer pre-roll, trailers, feature pre-roll
            results.Should().HaveCount(3);
            results[0].Path.Should().Be("/prerolls/trailer-preroll.mkv");
            results[2].Path.Should().Be("/prerolls/feature-preroll.mkv");
        }

        [Fact]
        public void Get_PreRollSelectorThrows_CatchesAndContinues()
        {
            // Use an invalid library GUID to trigger an exception in PreRollSelector
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary("not-a-guid")
                .WithFeaturePreRollLibrary("-")
                .WithNumberOfTrailers(0)
                .Build();

            var manager = new IntroManager(_mockLibraryManager.Object, config, _logger);
            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();

            // Should not throw — exception is caught internally
            var results = manager.Get(feature, user).ToList();

            results.Should().BeEmpty();
        }

        [Fact]
        public void Get_OnlyFeaturePreRoll_ReturnsJustFeaturePreRoll()
        {
            var featurePreRoll = new MovieBuilder()
                .WithPath("/prerolls/feature.mkv")
                .Build();

            var featureLibrary = Guid.NewGuid().ToString();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { featurePreRoll }.AsReadOnly());

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary("-")
                .WithFeaturePreRollLibrary(featureLibrary)
                .WithFeaturePreRollsRatingLimit(false)
                .WithNumberOfTrailers(0)
                .Build();

            var manager = new IntroManager(_mockLibraryManager.Object, config, _logger);
            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();

            var results = manager.Get(feature, user).ToList();

            results.Should().ContainSingle();
            results[0].Path.Should().Be("/prerolls/feature.mkv");
        }
    }
}
