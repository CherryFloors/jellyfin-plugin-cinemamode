using Jellyfin.Plugin.CinemaMode.Configuration;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class TrailerSelectionConfigBuilder
    {
        private bool _year;
        private bool _decade;
        private bool _genre;
        private bool _recentlyAdded;
        private bool _moreLikeThis;
        private bool _unplayed;

        public TrailerSelectionConfigBuilder WithYear() { _year = true; return this; }
        public TrailerSelectionConfigBuilder WithDecade() { _decade = true; return this; }
        public TrailerSelectionConfigBuilder WithGenre() { _genre = true; return this; }
        public TrailerSelectionConfigBuilder WithRecentlyAdded() { _recentlyAdded = true; return this; }
        public TrailerSelectionConfigBuilder WithMoreLikeThis() { _moreLikeThis = true; return this; }
        public TrailerSelectionConfigBuilder WithUnplayed() { _unplayed = true; return this; }

        public TrailerSelectionConfig Build()
        {
            return new TrailerSelectionConfig
            {
                Year = _year,
                Decade = _decade,
                Genre = _genre,
                RecentlyAdded = _recentlyAdded,
                MoreLikeThis = _moreLikeThis,
                Unplayed = _unplayed,
            };
        }
    }
}
