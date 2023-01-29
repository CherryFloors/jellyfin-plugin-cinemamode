using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CinemaMode.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string TrailerPreRollsPath { get; set; }

        public string FeaturePreRollsPath { get; set; }

        public int NumberOfTrailers { get; set; }

        public bool EnableTrailerPreroll { get; set; }

        public bool EnableFeaturePreroll { get; set; }

        public string TrailerPreRollsChannelName { get; } = "Trailer Pre-Rolls";

        public string FeaturePreRollsChannelName { get; } = "Feature Pre-Rolls";

        public PluginConfiguration()
        {
            TrailerPreRollsPath = string.Empty;
            FeaturePreRollsPath = string.Empty;
            NumberOfTrailers = 2;
            EnableTrailerPreroll = false;
            EnableFeaturePreroll = false;
        }
    }
}
