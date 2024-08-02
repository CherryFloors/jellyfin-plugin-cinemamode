using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CinemaMode.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.CinemaMode
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Cinema Mode";

        public override Guid Id => Guid.Parse("5fcefe1b-df1f-4596-ac57-f2f939c294c5");

        public static Plugin Instance { get; private set; }

        public PluginConfiguration PluginConfiguration => Configuration;

        public static IServerApplicationPaths ServerApplicationPaths { get; private set; }

        public static ILibraryManager LibraryManager { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager, IServerApplicationPaths serverApplicationPaths)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            LibraryManager = libraryManager;
            ServerApplicationPaths = serverApplicationPaths;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
