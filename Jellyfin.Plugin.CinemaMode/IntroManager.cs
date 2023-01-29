using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.CinemaMode
{
    public class IntroManager
    {
        private readonly Random _random = new Random();

        public List<Guid> exclusion_list = new List<Guid>();

        public BaseItem? GetRandomMovieTrailer(bool isplayed, User user)
        {
            // Query movies
            InternalItemsQuery q = new InternalItemsQuery(user);
            q.HasTrailer = true;
            q.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Movie };
            q.ExcludeItemIds = exclusion_list.ToArray();
            q.IsPlayed = isplayed;
            IReadOnlyCollection<BaseItem> movies = Plugin.LibraryManager.GetItemList(q);

            // Check for no results
            if (movies.Count == 0)
            {
                return null;
            }

            // Get trailers and id to exculsion list
            Movie movie = movies.ElementAt(_random.Next(movies.Count)) as Movie;
            exclusion_list.Add(movie.Id);
            IReadOnlyList<BaseItem> localTrailers = movie.LocalTrailers;
            return localTrailers.ElementAt(_random.Next(localTrailers.Count));
        }

        public BaseItem? GetRandomPreRoll(string preRollChannelName)
        {
            // Query movies
            InternalItemsQuery q = new InternalItemsQuery();
            q.Name = preRollChannelName;
            q.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Channel };
            IReadOnlyCollection<BaseItem> preRollChannel = Plugin.LibraryManager.GetItemList(q);

            // Check for no results
            if (preRollChannel.Count == 0)
            {
                return null;
            }

            // Check for multiple
            if (preRollChannel.Count > 1)
            {
                return null;
            }

            // Get pre rolls in that channel
            InternalItemsQuery q2 = new InternalItemsQuery();
            q2.TopParentIds = new Guid[] { preRollChannel.ElementAt(0).Id };
            q2.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Video };
            IReadOnlyCollection<BaseItem> preRolls = Plugin.LibraryManager.GetItemList(q2);
            
            return preRolls.ElementAt(_random.Next(preRolls.Count));
        }

        public IEnumerable<IntroInfo> Get(BaseItem item, User user)
        {
            // Return trailer pre roll
            if (Plugin.Instance.Configuration.EnableTrailerPreroll) 
            {
                BaseItem? b = GetRandomPreRoll(Plugin.Instance.Configuration.TrailerPreRollsChannelName);
                if (b != null)
                {
                    yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
                }
            }

            // Empty list
            List<IntroInfo> introInfoList = new List<IntroInfo>();

            // Exclude Base Item
            exclusion_list.Add(item.Id);

            // Try to get unplayed trailers
            for (int i = 0; i < Plugin.Instance.Configuration.NumberOfTrailers; i++)
            {
                BaseItem? b = GetRandomMovieTrailer(false, user);
                if (b == null)
                {   
                    // Move on to played movies
                    break;
                }
                yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
            }

            // Fill the gaps
            int empty_spots = Plugin.Instance.Configuration.NumberOfTrailers - introInfoList.Count;
            for (int i = 0; i < empty_spots; i++)
            {
                BaseItem? b = GetRandomMovieTrailer(true, user);
                if (b == null)
                {
                    // No played movies
                    break;
                }
                yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
            }

            // Return feature pre roll
            if (Plugin.Instance.Configuration.EnableFeaturePreroll) 
            {
                BaseItem? b = GetRandomPreRoll(Plugin.Instance.Configuration.FeaturePreRollsChannelName);
                if (b != null)
                {
                    yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
                }
            }
        }
    }
}
