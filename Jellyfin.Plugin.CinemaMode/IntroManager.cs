using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Jellyfin.Data.Enums;

#nullable enable

namespace Jellyfin.Plugin.CinemaMode
{
    
    class TrailerSelector
    {
        private List<Movie> Trailers { get; set; } = new List<Movie>(){ };
        private int Returned { get; set; } = 0;
        private Random RNG { get; }
        private Jellyfin.Plugin.CinemaMode.Configuration.PluginConfiguration Config { get; }
        private BaseItem Feature { get; }
        private User User { get; }

        private void QueryTrailers(bool IsPlayed)
        {
            InternalItemsQuery q = new InternalItemsQuery(this.User);
            q.HasTrailer = true;
            q.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Movie };
            q.IsPlayed = IsPlayed;
            q.ExcludeItemIds = new Guid[] { this.Feature.Id };
            if (this.Config.EnforceRatingLimit)
            {
                q.HasOfficialRating = true;
                q.MaxParentalRating = this.Feature.InheritedParentalRatingValue;
            }
            
            List<Movie> movies = Plugin.LibraryManager.GetItemList(q).OfType<Movie>().Where(x => x.LocalTrailers.Count > 0).ToList();

            if (movies is not null)
            {
                this.Trailers = movies;
            }
        }

        public TrailerSelector(BaseItem Feature, User User, Jellyfin.Plugin.CinemaMode.Configuration.PluginConfiguration Config)
        {
            this.RNG = new Random();
            this.Config = Config;
            this.Feature = Feature;
            this.User = User;
        }

        public IntroInfo PickAndPop()
        {
            int idx = this.RNG.Next(this.Trailers.Count);
            Movie movie = this.Trailers.ElementAt(idx);
            this.Trailers.RemoveAt(idx);

            int trailerID = this.RNG.Next(movie.LocalTrailers.Count);
            BaseItem item = movie.LocalTrailers.ElementAt(trailerID);
            return new IntroInfo { ItemId = item.Id, Path = item.Path };
        }
        
        public IEnumerable<IntroInfo> GetTrailers()
        {
            // Unplayed
            this.QueryTrailers(false);
            while (this.Trailers.Count != 0 && this.Returned < this.Config.NumberOfTrailers)
            {
                yield return this.PickAndPop();
                this.Returned++;                 
            }

            // Break if we have reached count
            if (this.Returned == this.Config.NumberOfTrailers)
            {
                yield break;
            }

            // Played
            this.QueryTrailers(true);
            while (this.Trailers.Count != 0 && this.Returned < this.Config.NumberOfTrailers)
            {
                yield return this.PickAndPop();
                this.Returned++;
            }
            
        }
    }

    public class IntroManager
    {
        private readonly Random _random = new Random();

        public BaseItem? GetRandomPreRoll(string preRollChannelPath, string preRollChannelName)
        {
            try
            {
                // Get pre rolls paths
                string[] preRollPaths = System.IO.Directory.GetFiles(preRollChannelPath);

                // If no paths, take early exit
                if (preRollPaths.Count() == 0)
                {
                    return null;
                }

                // Get chennel
                InternalItemsQuery q = new InternalItemsQuery();
                q.Name = preRollChannelName;
                q.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Folder };
                IReadOnlyCollection<BaseItem> preRollChannel = Plugin.LibraryManager.GetItemList(q);

                // Check for no results
                if (preRollChannel.Count != 1)
                {
                    return null;
                }

                // Get pre rolls in that channel
                InternalItemsQuery q2 = new InternalItemsQuery();
                q2.ParentId = preRollChannel.ElementAt(0).Id;
                q2.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Video };
                IReadOnlyCollection<BaseItem> preRolls = Plugin.LibraryManager.GetItemList(q2);

                // Filter out matches without bitrate and file path doesnt exist
                IEnumerable<BaseItem> goodPreRolls = preRolls.Where(p => p.TotalBitrate != null).Where(p => preRollPaths.Contains(p.Path));
                if (goodPreRolls.Count() == 0)
                {
                    return null;
                }

                // Return a random
                return goodPreRolls.ElementAt(_random.Next(goodPreRolls.Count()));
            }
            catch (System.Exception)
            {
                // likely a file permission error, default to none
                return null;
            }
            
        }

        public IEnumerable<IntroInfo> Get(BaseItem item, User user)
        {
            // Return trailer pre roll
            if (Plugin.Instance.Configuration.EnableTrailerPreroll) 
            {
                BaseItem? b = GetRandomPreRoll(Plugin.Instance.Configuration.TrailerPreRollsPath, Plugin.Instance.Configuration.TrailerPreRollsChannelName);
                if (b != null)
                {
                    yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
                }
            }

            // Return Trailers
            if (Plugin.Instance.Configuration.NumberOfTrailers > 0)
            {
                TrailerSelector trailerSelector = new TrailerSelector(item, user, Plugin.Instance.Configuration);
                foreach (IntroInfo trailer in trailerSelector.GetTrailers())
                {
                    yield return trailer;
                }
            }

            // Return feature pre roll
            if (Plugin.Instance.Configuration.EnableFeaturePreroll) 
            {
                BaseItem? b = GetRandomPreRoll(Plugin.Instance.Configuration.FeaturePreRollsPath, Plugin.Instance.Configuration.FeaturePreRollsChannelName);
                if (b != null)
                {
                    yield return new IntroInfo { ItemId = b.Id, Path = b.Path };
                }
            }
        }
    }
}
#nullable disable
