using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CinemaMode.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string TrailerPreRollsPath { get; set; }

        public string FeaturePreRollsPath { get; set; }

        public int NumberOfTrailers { get; set; }

        public PluginConfiguration()
        {
            TrailerPreRollsPath = string.Empty;
            FeaturePreRollsPath = string.Empty;
            NumberOfTrailers = 2;
        }
    }
}
