using Jellyfin.Plugin.CinemaMode.Configuration;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class SeasonalTagBuilder
    {
        private string _tag = "Halloween";
        private string _start = "October 1";
        private string _end = "November 1";

        public SeasonalTagBuilder WithTag(string tag) { _tag = tag; return this; }
        public SeasonalTagBuilder WithRange(string start, string end) { _start = start; _end = end; return this; }

        public SeasonalTagDefinition Build()
        {
            return new SeasonalTagDefinition
            {
                Tag = _tag,
                Start = _start,
                End = _end,
            };
        }
    }
}
