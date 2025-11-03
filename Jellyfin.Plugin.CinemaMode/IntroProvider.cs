using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
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
            Logger.LogDebug("|jellyfin-cinema-mode| GetIntros called for item: {ItemName} by user: {UserName}", item.Name, user.Username);

            // Check item type, for now just pre roll movies
            if (item is not MediaBrowser.Controller.Entities.Movies.Movie)
            {
                Logger.LogDebug("|jellyfin-cinema-mode| Item is not a movie, returning empty intros.");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            Logger.LogDebug("|jellyfin-cinema-mode| Item is a movie, fetching intros.");
            IntroManager introManager = new IntroManager(this.Logger);
            return Task.FromResult(introManager.Get(item, user));
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            Logger.LogDebug("|jellyfin-cinema-mode| GetAllIntroFiles called");
            // not implemented
            return Enumerable.Empty<string>();
        }
    }
}
