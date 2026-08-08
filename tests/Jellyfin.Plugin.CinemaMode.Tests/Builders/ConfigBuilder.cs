using System.Collections.Generic;
using Jellyfin.Plugin.CinemaMode.Configuration;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class ConfigBuilder
    {
        private readonly PluginConfiguration _config = new();

        public ConfigBuilder WithTrailerPreRollLibrary(string libraryId)
        {
            _config.TrailerPreRollsLibrary = libraryId;
            return this;
        }

        public ConfigBuilder WithFeaturePreRollLibrary(string libraryId)
        {
            _config.FeaturePreRollsLibrary = libraryId;
            return this;
        }

        public ConfigBuilder WithNumberOfTrailers(int count)
        {
            _config.NumberOfTrailers = count;
            return this;
        }

        public ConfigBuilder WithEnforceRatingLimitTrailers(bool enforce)
        {
            _config.EnforceRatingLimitTrailers = enforce;
            return this;
        }

        public ConfigBuilder WithTrailerPreRollsRatingLimit(bool enforce)
        {
            _config.TrailerPreRollsRatingLimit = enforce;
            return this;
        }

        public ConfigBuilder WithFeaturePreRollsRatingLimit(bool enforce)
        {
            _config.FeaturePreRollsRatingLimit = enforce;
            return this;
        }

        public ConfigBuilder WithTrailerPreRollsIgnoreOutOfSeason(bool ignore)
        {
            _config.TrailerPreRollsIgnoreOutOfSeason = ignore;
            return this;
        }

        public ConfigBuilder WithFeaturePreRollsIgnoreOutOfSeason(bool ignore)
        {
            _config.FeaturePreRollsIgnoreOutOfSeason = ignore;
            return this;
        }

        public ConfigBuilder WithTrailerConsumeMode(bool consume)
        {
            _config.TrailerConsumeMode = consume;
            return this;
        }

        public ConfigBuilder WithTrailerPreRollSelections(params PreRollSelectionConfig[] selections)
        {
            _config.TrailerPreRollsSelections = new List<PreRollSelectionConfig>(selections);
            return this;
        }

        public ConfigBuilder WithFeaturePreRollSelections(params PreRollSelectionConfig[] selections)
        {
            _config.FeaturePreRollsSelections = new List<PreRollSelectionConfig>(selections);
            return this;
        }

        public ConfigBuilder WithTrailerSelectionRules(params TrailerSelectionConfig[] rules)
        {
            _config.TrailerSelectionRules = new List<TrailerSelectionConfig>(rules);
            return this;
        }

        public ConfigBuilder WithSeasonalTags(params SeasonalTagDefinition[] tags)
        {
            _config.SeasonalTagDefinitions = new List<SeasonalTagDefinition>(tags);
            return this;
        }

        public PluginConfiguration Build()
        {
            return _config;
        }
    }
}
