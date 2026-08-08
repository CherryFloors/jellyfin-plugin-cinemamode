using Jellyfin.Plugin.CinemaMode.Configuration;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class PreRollSelectionConfigBuilder
    {
        private bool _name;
        private bool _year;
        private bool _decade;
        private bool _seasonal;
        private bool _genre;
        private bool _studios;
        private bool _allTags;

        public PreRollSelectionConfigBuilder WithName() { _name = true; return this; }
        public PreRollSelectionConfigBuilder WithYear() { _year = true; return this; }
        public PreRollSelectionConfigBuilder WithDecade() { _decade = true; return this; }
        public PreRollSelectionConfigBuilder WithSeasonal() { _seasonal = true; return this; }
        public PreRollSelectionConfigBuilder WithGenre() { _genre = true; return this; }
        public PreRollSelectionConfigBuilder WithStudios() { _studios = true; return this; }
        public PreRollSelectionConfigBuilder WithAllTags() { _allTags = true; return this; }

        public PreRollSelectionConfigBuilder All()
        {
            _name = _year = _decade = _seasonal = _genre = _studios = _allTags = true;
            return this;
        }

        public PreRollSelectionConfig Build()
        {
            return new PreRollSelectionConfig
            {
                Name = _name,
                Year = _year,
                Decade = _decade,
                Seasonal = _seasonal,
                Genre = _genre,
                Studios = _studios,
                AllTags = _allTags,
            };
        }
    }
}
