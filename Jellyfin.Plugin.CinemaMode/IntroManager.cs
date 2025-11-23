using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.CinemaMode.Configuration;

#nullable enable

namespace Jellyfin.Plugin.CinemaMode
{

    enum PreRollType
    {
        TrailerPreRoll,
        FeaturePreRoll,
    }

    class PreRollSelector
    {
        private PreRollType Category { get; } 
        private Random RNG { get; }
        private BaseItem Feature { get; }
        private User User { get; }
        private string PreRollLibrary { get; }
        private List<PreRollSelectionConfig> PreRollsSelections { get; }
        private bool IgnoreOutOfSeason { get; }
        private bool EnforceRatingLimit { get; }
        private List<SeasonalTagDefinition> SeasonalTagDefinitions { get; }
        private readonly ILogger Logger;


        public PreRollSelector(PreRollType Category, BaseItem Feature, User User, PluginConfiguration Config, ILogger logger)
        {
            this.Category = Category;
            this.RNG = new Random();
            this.Feature = Feature;
            this.User = User;
            this.Logger = logger;

            if (Category == PreRollType.TrailerPreRoll)
            {
                this.PreRollLibrary = Config.TrailerPreRollsLibrary;
                this.PreRollsSelections = Config.TrailerPreRollsSelections;
                this.EnforceRatingLimit = Config.TrailerPreRollsRatingLimit;
                this.SeasonalTagDefinitions = Config.SeasonalTagDefinitions;
                this.IgnoreOutOfSeason = Config.TrailerPreRollsIgnoreOutOfSeason;
            }
            else
            {
                this.PreRollLibrary = Config.FeaturePreRollsLibrary;
                this.PreRollsSelections = Config.FeaturePreRollsSelections;
                this.EnforceRatingLimit = Config.FeaturePreRollsRatingLimit;
                this.SeasonalTagDefinitions = Config.SeasonalTagDefinitions;
                this.IgnoreOutOfSeason = Config.FeaturePreRollsIgnoreOutOfSeason;
            }
        }

        public List<String> PreRollYearTags() 
        {
            if (!this.Feature.PremiereDate.HasValue)
            {
                return new List<string>();
            }
            
            String Year = this.Feature.PremiereDate.Value.Year.ToString();
            String Decade = $"{Year.Substring(0, 3)}0s";
            return new List<string>() { Year, Decade };
        }
        
        public bool InSeason(SeasonalTagDefinition seasonalTag, DateTime today)
        {
            DateTime startDate = DateTime.Parse(seasonalTag.Start);
            DateTime endDate = DateTime.Parse(seasonalTag.End);

            int yearDiff = endDate.Year - startDate.Year;
            startDate = new DateTime(today.Year, startDate.Month, startDate.Day);
            endDate = new DateTime(today.Year + yearDiff, endDate.Month, endDate.Day);

            if (today >= startDate && today <= endDate)
            {
                return true;
            }   
            return false;
        }

        public List<String> PreRollSeasonTags()
        {
            DateTime today = DateTime.Now;
            List<string> Tags = new List<string>();

            foreach (SeasonalTagDefinition seasonalTag in this.SeasonalTagDefinitions)
            {
                if (InSeason(seasonalTag, today))
                {
                    Tags.Add(seasonalTag.Tag);
                }   
            }

            return Tags;
        }

        private string[] OutOfSeasonTags()
        {
            DateTime today = DateTime.Now;
            List<string> Tags = new List<string>();

            foreach (SeasonalTagDefinition seasonalTag in this.SeasonalTagDefinitions)
            {
                if (!InSeason(seasonalTag, today))
                {
                    Tags.Add(seasonalTag.Tag);
                }   
            }
            return Tags.ToArray();
        }

        public Guid[] GetStudioIds()
        {
            List<Guid> ids = new List<Guid>();
            foreach (string studio in Feature.Studios)
            {
                InternalItemsQuery query = new InternalItemsQuery();
                query.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Studio };
                query.Name = studio;
                IReadOnlyList<BaseItem> items = Plugin.LibraryManager.GetItemList(query);
                foreach (BaseItem item in items)
                {
                    ids.Add(item.Id);
                } 
            }
            return ids.ToArray();
        }


        public InternalItemsQuery QueryBuilder(PreRollSelectionConfig? SelectionConfig)
        {
            InternalItemsQuery query = new InternalItemsQuery(this.User);
            query.ParentId = Guid.Parse(this.PreRollLibrary);
            query.Recursive = true;
            query.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Movie };

            if (this.EnforceRatingLimit)
            {
                query.MaxParentalRating = this.Feature.GetParentalRatingScore();
            }

            if (SelectionConfig == null)
            {
                return query;
            }

            if (SelectionConfig.Genre)
            {
                query.Genres = Feature.Genres;
            }

            if (SelectionConfig.Studios)
            {
                Guid[] ids = GetStudioIds();
                query.StudioIds = ids;
            }

            List<String> tags = new List<String>();
            if (SelectionConfig.Name)
            {
                tags.Add(Feature.Name);
            }

            if (SelectionConfig.Year && this.Feature.PremiereDate.HasValue)
            {
                tags.Add(this.Feature.PremiereDate.Value.Year.ToString());
            }

            if (SelectionConfig.Decade && this.Feature.PremiereDate.HasValue)
            {
                tags.Add($"{this.Feature.PremiereDate.Value.Year.ToString().Substring(0, 3)}0s");
            }

            if (SelectionConfig.Seasonal)
            {
                tags.AddRange(PreRollSeasonTags());
            }

            if (tags.Count > 0)
            {
                query.Tags = tags.ToArray();
            }

            return query;
        }

        public List<Movie> QueryPreRolls(PreRollSelectionConfig? SelectionConfig)
        {
            InternalItemsQuery query = QueryBuilder(SelectionConfig);
            IReadOnlyList<BaseItem> items = Plugin.LibraryManager.GetItemList(query);
            if (SelectionConfig != null && SelectionConfig.AllTags)
            {
                items = items.Where(pR => query.Tags.All(t => pR.Tags.Contains(t))).ToList();
            }

            if (this.IgnoreOutOfSeason)
            {
                string[] excludeTags = OutOfSeasonTags(); 
                if (excludeTags.Length > 0)
                {
                    items = items.Where(pR => !excludeTags.Any(t => pR.Tags.Contains(t))).ToList();
                }
            }
            
            return items.OfType<Movie>().ToList();
        }

        public IntroInfo? GetPreRoll()
        {
            List<Movie> preRolls;
            foreach (PreRollSelectionConfig SelectConfig in this.PreRollsSelections)
            {
                preRolls = QueryPreRolls(SelectConfig);
                if (preRolls.Count > 0) {
                    int idx = this.RNG.Next(preRolls.Count);
                    Video preRoll = preRolls.ElementAt(idx);
                    return new IntroInfo { ItemId = preRoll.Id, Path = preRoll.Path };
                }
            }

            preRolls = QueryPreRolls(null);
            if (preRolls.Count > 0) {
                int idx = this.RNG.Next(preRolls.Count);
                Video preRoll = preRolls.ElementAt(idx);
                return new IntroInfo { ItemId = preRoll.Id, Path = preRoll.Path };
            }

            this.Logger.LogInformation($"|jellyfin-cinema-mode| No Pre-Rolls Type: {this.Category} User: {this.User}");
            return null;

        }
    }

    class TrailerSelector
    {
        private List<Movie> Trailers { get; set; } = new List<Movie>() { };
        private int Returned { get; set; } = 0;
        private List<Guid> ShownTrailers = new List<Guid>() { };
        private Random RNG { get; }
        private PluginConfiguration Config { get; }
        private BaseItem Feature { get; }
        private User User { get; }
        private List<TrailerSelectionConfig> selectionRules { get; set; } = new List<TrailerSelectionConfig>() { };
        private readonly ILogger Logger;

        public TrailerSelector(BaseItem Feature, User User, Jellyfin.Plugin.CinemaMode.Configuration.PluginConfiguration Config, ILogger logger)
        {
            this.ShownTrailers.Add(Feature.Id);
            this.RNG = new Random();
            this.Config = Config;
            this.Feature = Feature;
            this.User = User;
            this.Logger = logger;
            foreach (TrailerSelectionConfig selectionConfig in Config.TrailerSelectionRules)
            {
               this.selectionRules.Add(selectionConfig); 
            }
        }

        private InternalItemsQuery BaseQuery(bool IsPlayed)
        {
            InternalItemsQuery baseQuery = new InternalItemsQuery(this.User);
            baseQuery.HasTrailer = true;
            baseQuery.IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Movie };
            baseQuery.ExcludeItemIds = this.ShownTrailers.ToArray();

            if (this.Config.EnforceRatingLimitTrailers)
            {
                baseQuery.HasOfficialRating = true;
                baseQuery.HasParentalRating = true;
                baseQuery.MinParentalRating = new ParentalRatingScore(0, 0);
                baseQuery.MaxParentalRating = this.Feature.GetParentalRatingScore();
            }

            baseQuery.IsPlayed = IsPlayed;

            return baseQuery;
        }

        private InternalItemsQuery BuildQuery(TrailerSelectionConfig config)
        {
            InternalItemsQuery configQuery = BaseQuery(!config.Unplayed);

            if (config.Year && Feature.PremiereDate.HasValue)
            {
                int year = Feature.PremiereDate.Value.Year;
                configQuery.MinPremiereDate = new DateTime(year, 1, 1);
                configQuery.MaxPremiereDate = new DateTime(year, 12, 31);
            }

            if (config.Decade && Feature.PremiereDate.HasValue)
            {
                int year_start = (Feature.PremiereDate.Value.Year / 10) * 10;
                int year_end = year_start + 9;
                configQuery.MinPremiereDate = new DateTime(year_start, 1, 1);
                configQuery.MaxPremiereDate = new DateTime(year_end, 12, 31);
            }
            
            if (config.Genre)
            {
                configQuery.Genres = Feature.Genres;
            }

            if (config.RecentlyAdded)
            {
                configQuery.MinDateCreated = DateTime.Today.AddMonths(-1);
            }
            
            if (config.MoreLikeThis)
            {
                configQuery.Genres = this.Feature.Genres;
                configQuery.Tags = this.Feature.Tags;
                configQuery.EnableGroupByMetadataKey = true;
            }

            return configQuery;
        }

        private void QueryTrailers(InternalItemsQuery query)
        {
            IReadOnlyList<BaseItem> baseItems = Plugin.LibraryManager.GetItemList(query);
            IEnumerable<Movie> moviesIter = baseItems.OfType<Movie>().Where(x => x.LocalTrailers.Count > 0);

            // TODO: Remove when jf team fixes min rating query
            if (this.Config.EnforceRatingLimitTrailers)
            {
                moviesIter = moviesIter.Where(x => x.InheritedParentalRatingValue.HasValue);
            }

            List<Movie> movies = moviesIter.ToList();
            if (movies is not null)
            {
                this.Trailers = movies;
            } else {
                this.Logger.LogInformation($"|jellyfin-cinema-mode| No trailer found: {this.Feature.Name} {this.Feature.InheritedParentalRatingValue})");
            }
        }

        private void SelectionRuleQuery()
        {
            this.Trailers = new List<Movie>() { };
            while (this.selectionRules.Count != 0 && this.Trailers.Count == 0)
            {
                TrailerSelectionConfig trailerSelection = this.selectionRules.ElementAt(0);
                this.selectionRules.RemoveAt(0); 
                this.QueryTrailers(this.BuildQuery(trailerSelection));
            }
        }

        public IntroInfo PickAndPop()
        {
            int idx = this.RNG.Next(this.Trailers.Count);
            Movie movie = this.Trailers.ElementAt(idx);
            this.ShownTrailers.Add(movie.Id);
            this.Trailers.RemoveAt(idx);

            int trailerID = this.RNG.Next(movie.LocalTrailers.Count);
            BaseItem item = movie.LocalTrailers.ElementAt(trailerID);
            return new IntroInfo { ItemId = item.Id, Path = item.Path };
        }

        public IEnumerable<IntroInfo> GetTrailers()
        {
            // Early break if we are enforcing trailer limit and the feature is "unrated" or has no rating
            if (this.Config.EnforceRatingLimitTrailers && this.Feature.InheritedParentalRatingValue == null)
            {
                this.Logger.LogInformation("|jellyfin-cinema-mode| Feature has no rating, skipping trailers");
                yield break;
            }

            // Selection Rules
            this.SelectionRuleQuery();
            while (this.Trailers.Count != 0 && this.Returned < this.Config.NumberOfTrailers)
            {
                yield return this.PickAndPop();
                this.Returned++;

                if (this.Config.TrailerConsumeMode | this.Trailers.Count == 0)
                {
                   SelectionRuleQuery(); 
                }
            }
             
            // Break if we have reached count
            if (this.Returned == this.Config.NumberOfTrailers)
            {
                yield break;
            }

            // Unplayed
            this.QueryTrailers(this.BaseQuery(false));
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
            this.QueryTrailers(this.BaseQuery(true));
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

        private readonly ILogger Logger;

        public IntroManager(ILogger logger)
        {
            this.Logger = logger;
        }

        public IEnumerable<IntroInfo> Get(BaseItem item, User user)
        {

            if (Plugin.Instance.Configuration.TrailerPreRollsLibrary != "-")
            {
                IntroInfo? trailerPreRoll = null;
                try
                {
                    PreRollSelector preRollSelector = new PreRollSelector(PreRollType.TrailerPreRoll, item, user, Plugin.Instance.Configuration, this.Logger);
                    trailerPreRoll = preRollSelector.GetPreRoll();
                }
                catch (System.Exception e)
                {
                    this.Logger.LogError("|jellyfin-cinema-mode| Exception encountered fetching Trailer Pre-Roll");
                    this.Logger.LogError(e.StackTrace);
                }

                if (trailerPreRoll != null)
                {
                    yield return trailerPreRoll;
                }
            }

            if (Plugin.Instance.Configuration.NumberOfTrailers > 0)
            {
                List<IntroInfo> trailers = new List<IntroInfo>();
                try
                {
                    TrailerSelector trailerSelector = new TrailerSelector(item, user, Plugin.Instance.Configuration, this.Logger);
                    trailers = trailerSelector.GetTrailers().ToList();
                }
                catch (System.Exception e)
                {
                    this.Logger.LogError("|jellyfin-cinema-mode| Exception encountered fetching Trailers");
                    this.Logger.LogError(e.StackTrace);
                }

                foreach (IntroInfo trailer in trailers)
                {
                    yield return trailer;
                }
            }

            if (Plugin.Instance.Configuration.FeaturePreRollsLibrary != "-")
            {
                IntroInfo? featurePreRoll = null;
                try
                {
                    PreRollSelector preRollSelector = new PreRollSelector(PreRollType.FeaturePreRoll, item, user, Plugin.Instance.Configuration, this.Logger);
                    featurePreRoll = preRollSelector.GetPreRoll();
                }
                catch (System.Exception e)
                {
                    this.Logger.LogError("|jellyfin-cinema-mode| Exception encountered fetching Feature Pre-Roll");
                    this.Logger.LogError(e.StackTrace);
                }

                if (featurePreRoll != null)
                {
                    yield return featurePreRoll;
                }
            }
        }
    }
}
#nullable disable
