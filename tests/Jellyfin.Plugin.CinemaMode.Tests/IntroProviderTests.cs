using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.CinemaMode.Tests.Builders;
using Jellyfin.Plugin.CinemaMode.Tests.Fixtures;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.CinemaMode.Tests
{
    [Collection("Jellyfin")]
    public class IntroProviderTests
    {
        public IntroProviderTests(JellyfinFixture fixture)
        {
        }

        [Fact]
        public async Task GetIntros_NonMovieItem_ReturnsEmpty()
        {
            var logger = new NullLoggerFactory().CreateLogger<IntroProvider>();
            var provider = new IntroProvider(logger);

            var episode = new Episode { Name = "Test Episode" };
            var user = new UserBuilder().Build();

            var result = await provider.GetIntros(episode, user);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetIntros_SeriesItem_ReturnsEmpty()
        {
            var logger = new NullLoggerFactory().CreateLogger<IntroProvider>();
            var provider = new IntroProvider(logger);

            var series = new Series { Name = "Test Series" };
            var user = new UserBuilder().Build();

            var result = await provider.GetIntros(series, user);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetAllIntroFiles_ReturnsEmpty()
        {
            var logger = new NullLoggerFactory().CreateLogger<IntroProvider>();
            var provider = new IntroProvider(logger);

            var result = provider.GetAllIntroFiles();

            result.Should().BeEmpty();
        }
    }
}
