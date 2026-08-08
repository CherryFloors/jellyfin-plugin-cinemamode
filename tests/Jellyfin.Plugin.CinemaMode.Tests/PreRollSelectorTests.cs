using System;
using System.Collections.Generic;
using FluentAssertions;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CinemaMode.Configuration;
using Jellyfin.Plugin.CinemaMode.Tests.Builders;
using Jellyfin.Plugin.CinemaMode.Tests.Fixtures;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.CinemaMode.Tests
{
    [Collection("Jellyfin")]
    public class PreRollSelectorTests
    {
        private static readonly string LibraryId = Guid.NewGuid().ToString();
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly ILogger _logger;

        public PreRollSelectorTests(JellyfinFixture fixture)
        {
            _mockLibraryManager = new Mock<ILibraryManager>();
            _logger = NullLogger.Instance;
        }

        private PreRollSelector CreateSelector(
            Movie feature,
            PluginConfiguration? config = null,
            TimeProvider? timeProvider = null,
            PreRollType type = PreRollType.TrailerPreRoll)
        {
            var cfg = config ?? new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithFeaturePreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithFeaturePreRollsRatingLimit(false)
                .Build();

            var user = new UserBuilder().Build();

            return new PreRollSelector(
                type, feature, user, cfg,
                _mockLibraryManager.Object,
                timeProvider ?? TimeProvider.System,
                _logger);
        }

        // ── PreRollYearTags ──

        [Fact]
        public void PreRollYearTags_WithPremiereDate_ReturnsYearAndDecade()
        {
            var feature = new MovieBuilder().WithYear(1988).Build();
            var selector = CreateSelector(feature);

            var tags = selector.PreRollYearTags();

            tags.Should().BeEquivalentTo("1988", "1980s");
        }

        [Fact]
        public void PreRollYearTags_WithoutPremiereDate_ReturnsEmptyList()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);

            var tags = selector.PreRollYearTags();

            tags.Should().BeEmpty();
        }

        [Fact]
        public void PreRollYearTags_Year2000_ReturnsCorrectDecade()
        {
            var feature = new MovieBuilder().WithYear(2000).Build();
            var selector = CreateSelector(feature);

            var tags = selector.PreRollYearTags();

            tags.Should().BeEquivalentTo("2000", "2000s");
        }

        [Fact]
        public void PreRollYearTags_Year2019_Returns2010sDecade()
        {
            var feature = new MovieBuilder().WithYear(2019).Build();
            var selector = CreateSelector(feature);

            var tags = selector.PreRollYearTags();

            tags.Should().BeEquivalentTo("2019", "2010s");
        }

        // ── InSeason ──

        [Fact]
        public void InSeason_WithinRange_ReturnsTrue()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Halloween")
                .WithRange("October 1", "November 1")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 10, 15));

            result.Should().BeTrue();
        }

        [Fact]
        public void InSeason_OutsideRange_ReturnsFalse()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Halloween")
                .WithRange("October 1", "November 1")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 3, 15));

            result.Should().BeFalse();
        }

        [Fact]
        public void InSeason_ExactStartDate_ReturnsTrue()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Halloween")
                .WithRange("October 1", "November 1")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 10, 1));

            result.Should().BeTrue();
        }

        [Fact]
        public void InSeason_ExactEndDate_ReturnsTrue()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Halloween")
                .WithRange("October 1", "November 1")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 11, 1));

            result.Should().BeTrue();
        }

        [Fact]
        public void InSeason_CrossYearBoundary_WithinRange_ReturnsTrue()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            // yearDiff=1 needed for cross-year ranges
            var tag = new SeasonalTagBuilder()
                .WithTag("Christmas")
                .WithRange("November 15 2000", "January 15 2001")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 12, 25));

            result.Should().BeTrue();
        }

        [Fact]
        public void InSeason_CrossYearBoundary_JanuaryDate_ReturnsTrue()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Christmas")
                .WithRange("November 15 2000", "January 15 2001")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 1, 10));

            result.Should().BeTrue();
        }

        [Fact]
        public void InSeason_CrossYearBoundary_OutsideRange_ReturnsFalse()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);
            var tag = new SeasonalTagBuilder()
                .WithTag("Christmas")
                .WithRange("November 15 2000", "January 15 2001")
                .Build();

            var result = selector.InSeason(tag, new DateTime(2024, 6, 1));

            result.Should().BeFalse();
        }

        // ── PreRollSeasonTags ──

        [Fact]
        public void PreRollSeasonTags_ReturnsOnlyInSeasonTags()
        {
            var halloween = new SeasonalTagBuilder()
                .WithTag("Halloween").WithRange("October 1", "November 1").Build();
            var christmas = new SeasonalTagBuilder()
                .WithTag("Christmas").WithRange("December 1", "January 5").Build();

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithSeasonalTags(halloween, christmas)
                .Build();

            var feature = new MovieBuilder().Build();
            var timeProvider = new FixedTimeProvider(new DateTime(2024, 10, 15));
            var selector = CreateSelector(feature, config, timeProvider);

            var tags = selector.PreRollSeasonTags();

            tags.Should().ContainSingle().Which.Should().Be("Halloween");
        }

        [Fact]
        public void PreRollSeasonTags_NoSeasonalTags_ReturnsEmpty()
        {
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .Build();

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature, config);

            var tags = selector.PreRollSeasonTags();

            tags.Should().BeEmpty();
        }

        [Fact]
        public void PreRollSeasonTags_MultipleInSeason_ReturnsAll()
        {
            var halloween = new SeasonalTagBuilder()
                .WithTag("Halloween").WithRange("October 1", "November 1").Build();
            var fallVibes = new SeasonalTagBuilder()
                .WithTag("Fall").WithRange("September 1", "November 30").Build();

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithSeasonalTags(halloween, fallVibes)
                .Build();

            var feature = new MovieBuilder().Build();
            var timeProvider = new FixedTimeProvider(new DateTime(2024, 10, 15));
            var selector = CreateSelector(feature, config, timeProvider);

            var tags = selector.PreRollSeasonTags();

            tags.Should().BeEquivalentTo("Halloween", "Fall");
        }

        // ── QueryBuilder ──

        [Fact]
        public void QueryBuilder_NullConfig_ReturnsBaseQuery()
        {
            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);

            var query = selector.QueryBuilder(null);

            query.ParentId.Should().Be(Guid.Parse(LibraryId));
            query.Recursive.Should().BeTrue();
            query.IncludeItemTypes.Should().Contain(BaseItemKind.Movie);
        }

        [Fact]
        public void QueryBuilder_GenreConfig_SetsGenresFromFeature()
        {
            var feature = new MovieBuilder()
                .WithGenres("Action", "Thriller")
                .Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Genres.Should().BeEquivalentTo("Action", "Thriller");
        }

        [Fact]
        public void QueryBuilder_NameConfig_AddsNameTag()
        {
            var feature = new MovieBuilder().WithName("Die Hard").Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithName().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().Contain("Die Hard");
        }

        [Fact]
        public void QueryBuilder_YearConfig_AddsYearTag()
        {
            var feature = new MovieBuilder().WithYear(1988).Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithYear().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().Contain("1988");
        }

        [Fact]
        public void QueryBuilder_DecadeConfig_AddsDecadeTag()
        {
            var feature = new MovieBuilder().WithYear(1988).Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithDecade().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().Contain("1980s");
        }

        [Fact]
        public void QueryBuilder_SeasonalConfig_AddsSeasonalTags()
        {
            var christmas = new SeasonalTagBuilder()
                .WithTag("Christmas").WithRange("December 1 2000", "January 5 2001").Build();

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithSeasonalTags(christmas)
                .Build();

            var feature = new MovieBuilder().Build();
            var timeProvider = new FixedTimeProvider(new DateTime(2024, 12, 25));
            var selector = CreateSelector(feature, config, timeProvider);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithSeasonal().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().Contain("Christmas");
        }

        [Fact]
        public void QueryBuilder_MultipleFlags_CombinesTags()
        {
            var feature = new MovieBuilder()
                .WithName("Die Hard")
                .WithYear(1988)
                .Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder()
                .WithName()
                .WithYear()
                .WithDecade()
                .Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().Contain("Die Hard");
            query.Tags.Should().Contain("1988");
            query.Tags.Should().Contain("1980s");
        }

        [Fact]
        public void QueryBuilder_NoTagFlags_DoesNotSetTags()
        {
            var feature = new MovieBuilder().WithGenres("Action").Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.Tags.Should().BeNullOrEmpty();
        }

        [Fact]
        public void QueryBuilder_StudioConfig_QueriesLibraryForStudioIds()
        {
            var studioId = Guid.NewGuid();
            var studioItem = new Movie { Id = studioId, Name = "20th Century Fox" };

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                    q => q.Name == "20th Century Fox" && q.IncludeItemTypes.Length > 0)))
                .Returns(new List<BaseItem> { studioItem }.AsReadOnly());

            var feature = new MovieBuilder()
                .WithStudios("20th Century Fox")
                .Build();
            var selector = CreateSelector(feature);
            var selectionConfig = new PreRollSelectionConfigBuilder().WithStudios().Build();

            var query = selector.QueryBuilder(selectionConfig);

            query.StudioIds.Should().Contain(studioId);
        }

        [Fact]
        public void QueryBuilder_NoRatingEnforcement_DoesNotSetMaxParentalRating()
        {
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .Build();

            var feature = new MovieBuilder().WithRating(10).Build();
            var selector = CreateSelector(feature, config);

            var query = selector.QueryBuilder(null);

            query.MaxParentalRating.Should().BeNull();
        }

        // ── QueryPreRolls ──

        [Fact]
        public void QueryPreRolls_ReturnsMoviesFromLibrary()
        {
            var preRoll = new MovieBuilder()
                .WithName("Pre-Roll 1")
                .WithPath("/prerolls/1.mkv")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { preRoll }.AsReadOnly());

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);

            var results = selector.QueryPreRolls(null);

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Pre-Roll 1");
        }

        [Fact]
        public void QueryPreRolls_NoResults_ReturnsEmptyList()
        {
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem>().AsReadOnly());

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);

            var results = selector.QueryPreRolls(null);

            results.Should().BeEmpty();
        }

        [Fact]
        public void QueryPreRolls_FiltersNonMovieItems()
        {
            var video = new Video { Id = Guid.NewGuid(), Name = "Not a movie" };
            var movie = new MovieBuilder().WithName("Real Movie").Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { video, movie }.AsReadOnly());

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature);

            var results = selector.QueryPreRolls(null);

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Real Movie");
        }

        [Fact]
        public void QueryPreRolls_AllTags_FiltersItemsMissingTags()
        {
            var matchAll = new MovieBuilder()
                .WithName("Match")
                .WithTags("Die Hard", "1988")
                .Build();
            var matchPartial = new MovieBuilder()
                .WithName("Partial")
                .WithTags("Die Hard")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { matchAll, matchPartial }.AsReadOnly());

            var feature = new MovieBuilder()
                .WithName("Die Hard")
                .WithYear(1988)
                .Build();

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .Build();

            var selector = CreateSelector(feature, config);
            var selectionConfig = new PreRollSelectionConfigBuilder()
                .WithName()
                .WithYear()
                .WithAllTags()
                .Build();

            var results = selector.QueryPreRolls(selectionConfig);

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Match");
        }

        [Fact]
        public void QueryPreRolls_IgnoreOutOfSeason_ExcludesOutOfSeasonItems()
        {
            var halloweenPreRoll = new MovieBuilder()
                .WithName("Halloween Pre-Roll")
                .WithTags("Halloween")
                .Build();
            var genericPreRoll = new MovieBuilder()
                .WithName("Generic Pre-Roll")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { halloweenPreRoll, genericPreRoll }.AsReadOnly());

            var halloween = new SeasonalTagBuilder()
                .WithTag("Halloween").WithRange("October 1", "November 1").Build();

            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithTrailerPreRollsIgnoreOutOfSeason(true)
                .WithSeasonalTags(halloween)
                .Build();

            var feature = new MovieBuilder().Build();
            var timeProvider = new FixedTimeProvider(new DateTime(2024, 6, 15));
            var selector = CreateSelector(feature, config, timeProvider);

            var results = selector.QueryPreRolls(null);

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Generic Pre-Roll");
        }

        // ── GetPreRoll ──

        [Fact]
        public void GetPreRoll_FirstSelectionConfigMatches_ReturnsResult()
        {
            var preRoll = new MovieBuilder()
                .WithName("Matched Pre-Roll")
                .WithPath("/prerolls/matched.mkv")
                .WithGenres("Action")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { preRoll }.AsReadOnly());

            var selectionConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithTrailerPreRollSelections(selectionConfig)
                .Build();

            var feature = new MovieBuilder().WithGenres("Action").Build();
            var selector = CreateSelector(feature, config);

            var result = selector.GetPreRoll();

            result.Should().NotBeNull();
            result!.Path.Should().Be("/prerolls/matched.mkv");
            result.ItemId.Should().Be(preRoll.Id);
        }

        [Fact]
        public void GetPreRoll_FirstConfigEmpty_FallsToSecondConfig()
        {
            var preRoll = new MovieBuilder()
                .WithName("Second Config Match")
                .WithPath("/prerolls/second.mkv")
                .Build();

            var callCount = 0;
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1) return new List<BaseItem>().AsReadOnly();
                    return new List<BaseItem> { preRoll }.AsReadOnly();
                });

            var firstConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();
            var secondConfig = new PreRollSelectionConfigBuilder().WithYear().Build();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithTrailerPreRollSelections(firstConfig, secondConfig)
                .Build();

            var feature = new MovieBuilder().WithYear(1988).WithGenres("Action").Build();
            var selector = CreateSelector(feature, config);

            var result = selector.GetPreRoll();

            result.Should().NotBeNull();
            result!.Path.Should().Be("/prerolls/second.mkv");
        }

        [Fact]
        public void GetPreRoll_AllConfigsEmpty_FallsToNullConfig()
        {
            var fallbackPreRoll = new MovieBuilder()
                .WithName("Fallback")
                .WithPath("/prerolls/fallback.mkv")
                .Build();

            var callCount = 0;
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount <= 1) return new List<BaseItem>().AsReadOnly();
                    return new List<BaseItem> { fallbackPreRoll }.AsReadOnly();
                });

            var selectionConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithTrailerPreRollSelections(selectionConfig)
                .Build();

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature, config);

            var result = selector.GetPreRoll();

            result.Should().NotBeNull();
            result!.Path.Should().Be("/prerolls/fallback.mkv");
        }

        [Fact]
        public void GetPreRoll_NoPreRollsAnywhere_ReturnsNull()
        {
            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem>().AsReadOnly());

            var selectionConfig = new PreRollSelectionConfigBuilder().WithGenre().Build();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(LibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithTrailerPreRollSelections(selectionConfig)
                .Build();

            var feature = new MovieBuilder().Build();
            var selector = CreateSelector(feature, config);

            var result = selector.GetPreRoll();

            result.Should().BeNull();
        }

        [Fact]
        public void GetPreRoll_UsesFeaturePreRollConfig_WhenTypeIsFeaturePreRoll()
        {
            var preRoll = new MovieBuilder()
                .WithPath("/prerolls/feature.mkv")
                .Build();

            _mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { preRoll }.AsReadOnly());

            var featureLibraryId = Guid.NewGuid().ToString();
            var config = new ConfigBuilder()
                .WithTrailerPreRollLibrary(Guid.NewGuid().ToString())
                .WithFeaturePreRollLibrary(featureLibraryId)
                .WithTrailerPreRollsRatingLimit(false)
                .WithFeaturePreRollsRatingLimit(false)
                .Build();

            var feature = new MovieBuilder().Build();
            var user = new UserBuilder().Build();
            var selector = new PreRollSelector(
                PreRollType.FeaturePreRoll, feature, user, config,
                _mockLibraryManager.Object, TimeProvider.System, _logger);

            var result = selector.GetPreRoll();

            result.Should().NotBeNull();
            _mockLibraryManager.Verify(m => m.GetItemList(
                It.Is<InternalItemsQuery>(q => q.ParentId == Guid.Parse(featureLibraryId))),
                Times.AtLeastOnce);
        }
    }
}
