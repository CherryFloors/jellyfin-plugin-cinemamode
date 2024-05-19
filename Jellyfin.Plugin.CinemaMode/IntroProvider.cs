using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaMode
{
    public class IntroProvider : IIntroProvider
    {
        public string Name { get; } = "CinemaMode";

        public readonly ILogger<IntroProvider> Logger;

        public IntroProvider(ILogger<IntroProvider> logger)
        {
            this.Logger = logger;
        }

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            // Check item type, for now just pre roll movies
            if (item is not MediaBrowser.Controller.Entities.Movies.Movie)
            {
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            IntroManager introManager = new IntroManager(this.Logger);
            return Task.FromResult(introManager.Get(item, user));
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            // not implemented
            return Enumerable.Empty<string>();
        }
    }
}
