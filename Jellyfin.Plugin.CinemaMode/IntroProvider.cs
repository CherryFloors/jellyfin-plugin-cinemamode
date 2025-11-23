using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaMode
{
    /// <summary>
    /// Provides intro content (trailers, pre-rolls) for movies in Jellyfin.
    /// Supports filtering by specific library names.
    /// </summary>
    public class IntroProvider : IIntroProvider
    {
        public string Name { get; } = "CinemaMode";

        private readonly ILogger<IntroProvider> Logger;

        /// <summary>
        /// The names of the libraries to filter intros for. 
        /// If null or empty, intros are provided for all movies.
        /// </summary>
        public List<string> TargetLibraryNames { get; set; } = new List<string>();

        /// <summary>
        /// Cached target library names to avoid repeated configuration parsing.
        /// </summary>
        private List<string> _cachedTargetLibraryNames;

        /// <summary>
        /// Cached configuration value to detect changes.
        /// </summary>
        private string _cachedIncludedLibraries;

        /// <summary>
        /// Flag to indicate if the cache needs to be refreshed.
        /// </summary>
        private bool _cacheNeedsRefresh = true;

        public IntroProvider(ILogger<IntroProvider> logger)
        {
            this.Logger = logger;
            
            // Register this instance with the Plugin for cache management
            if (Plugin.Instance != null)
            {
                Plugin.IntroProviderInstance = this;
                this.Logger.LogDebug("IntroProvider registered with Plugin instance for cache management");
            }
            
            // Log initialization with debug information
            this.Logger.LogInformation($"CinemaMode IntroProvider initialized");
            this.Logger.LogDebug("Debug logging enabled for CinemaMode IntroProvider");
            this.Logger.LogDebug($"Logger type: {logger.GetType().Name}");
            this.Logger.LogDebug($"Plugin instance available: {Plugin.Instance != null}");
        }

        /// <summary>
        /// Clears the cache to force a refresh of target library names on next access.
        /// This should be called when the plugin configuration is updated.
        /// </summary>
        public void ClearCache()
        {
            this.Logger.LogDebug("Clearing target library names cache");
            _cacheNeedsRefresh = true;
            _cachedTargetLibraryNames = null;
            _cachedIncludedLibraries = null;
        }

        /// <summary>
        /// Gets the cached target library names, refreshing the cache if needed.
        /// Automatically detects configuration changes and refreshes the cache.
        /// </summary>
        /// <returns>List of target library names</returns>
        private List<string> GetTargetLibraryNames()
        {
            var currentIncludedLibraries = Plugin.Instance?.Configuration?.IncludedLibraries ?? "";
            
            // Check if configuration has changed
            if (_cacheNeedsRefresh || _cachedTargetLibraryNames == null || 
                _cachedIncludedLibraries != currentIncludedLibraries)
            {
                this.Logger.LogDebug("Refreshing target library names cache due to configuration change");
                _cachedTargetLibraryNames = LoadTargetLibraryNames();
                _cachedIncludedLibraries = currentIncludedLibraries;
                _cacheNeedsRefresh = false;
                this.Logger.LogDebug($"Cached target libraries: [{string.Join(", ", _cachedTargetLibraryNames)}]");
            }
            
            return _cachedTargetLibraryNames;
        }

        /// <summary>
        /// Loads target library names from the plugin configuration.
        /// Parses the comma-separated IncludedLibraries string into a list.
        /// </summary>
        private List<string> LoadTargetLibraryNames()
        {
            try
            {
                if (Plugin.Instance?.Configuration?.IncludedLibraries != null)
                {
                    var includedLibraries = Plugin.Instance.Configuration.IncludedLibraries.Trim();
                    if (!string.IsNullOrEmpty(includedLibraries))
                    {
                        return includedLibraries
                            .Split(',')
                            .Select(lib => lib.Trim())
                            .Where(lib => !string.IsNullOrEmpty(lib))
                            .ToList();
                    }
                }
                return new List<string>();
            }
            catch (System.Exception ex)
            {
                this.Logger.LogError($"Error loading target library names from configuration: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets intro content for the specified item and user.
        /// Only provides intros for movies in the target libraries (if specified).
        /// </summary>
        /// <param name="item">The item to get intros for</param>
        /// <param name="user">The user requesting intros</param>
        /// <returns>Collection of intro information</returns>
        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            this.Logger.LogDebug($"GetIntros called for item: '{item.Name}' (ID: {item.Id}) by user: '{user.Username}'");

            // Only process movies
            if (item is not MediaBrowser.Controller.Entities.Movies.Movie)
            {
                this.Logger.LogDebug($"Skipping intros for item '{item.Name}' - not a movie (type: {item.GetType().Name})");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            this.Logger.LogDebug($"Item '{item.Name}' is a movie, proceeding with library check");

            // Get cached target library names
            var targetLibraryNames = GetTargetLibraryNames();

            // Apply library filtering if enabled
            if (targetLibraryNames != null && targetLibraryNames.Any())
            {
                if (!IsItemInTargetLibraries(item, targetLibraryNames))
                {
                    return Task.FromResult(Enumerable.Empty<IntroInfo>());
                }
            }
            else
            {
                this.Logger.LogDebug("Library filtering disabled (TargetLibraryNames is null or empty), proceeding with intros for all movies");
            }

            // Get intros from IntroManager
            this.Logger.LogDebug($"Creating IntroManager and getting intros for item '{item.Name}'");
            var introManager = new IntroManager(this.Logger);
            var intros = introManager.Get(item, user);
            var introList = intros.ToList();
            
            this.Logger.LogInformation($"Found {introList.Count} intros for item '{item.Name}' in target libraries [{string.Join(", ", targetLibraryNames)}]");
            foreach (var intro in introList)
            {
                this.Logger.LogDebug($"Intro: ItemId={intro.ItemId}, Path={intro.Path}");
            }

            return Task.FromResult(introList.AsEnumerable());
        }

        /// <summary>
        /// Checks if the item belongs to any of the target libraries.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <param name="targetLibraryNames">The list of target library names</param>
        /// <returns>True if the item is in any of the target libraries, false otherwise</returns>
        private bool IsItemInTargetLibraries(BaseItem item, List<string> targetLibraryNames)
        {
            this.Logger.LogDebug($"Library filtering enabled. Target libraries: [{string.Join(", ", targetLibraryNames)}]");
            
            var library = GetLibraryFromItem(item);
            if (library == null)
            {
                this.Logger.LogWarning($"Could not determine library for item '{item.Name}' (Path: {item.Path})");
                return false;
            }

            this.Logger.LogDebug($"Item '{item.Name}' found in library: '{library.Name}'");

            if (!targetLibraryNames.Any(targetName => targetName.Equals(library.Name, System.StringComparison.OrdinalIgnoreCase)))
            {
                this.Logger.LogInformation($"Skipping intros for item '{item.Name}' - in library '{library.Name}' but target libraries are [{string.Join(", ", targetLibraryNames)}]");
                return false;
            }

            this.Logger.LogInformation($"Item '{item.Name}' matches target library '{library.Name}', proceeding with intros");
            return true;
        }

        /// <summary>
        /// Finds the library that contains the specified item.
        /// </summary>
        /// <param name="item">The item to find the library for</param>
        /// <returns>The library folder containing the item, or null if not found</returns>
        private MediaBrowser.Controller.Entities.CollectionFolder GetLibraryFromItem(BaseItem item)
        {
            this.Logger.LogDebug($"Getting library for item: '{item.Name}' (Path: {item.Path})");
            
            try
            {
                var libraries = Plugin.LibraryManager.GetVirtualFolders();
                this.Logger.LogDebug($"Found {libraries.Count()} total libraries");

                foreach (var library in libraries)
                {
                    this.Logger.LogDebug($"Checking library: '{library.Name}' (ID: {library.ItemId}, Type: {library.CollectionType})");
                    
                    if (library.CollectionType.ToString().Equals("movies", System.StringComparison.OrdinalIgnoreCase))
                    {
                        this.Logger.LogDebug($"Library '{library.Name}' is a movie library, checking if item belongs to it");
                        
                        var libraryFolder = Plugin.LibraryManager.GetItemById(library.ItemId) as MediaBrowser.Controller.Entities.CollectionFolder;
                        if (libraryFolder == null)
                        {
                            this.Logger.LogWarning($"Could not get library folder for library '{library.Name}' (ID: {library.ItemId})");
                            continue;
                        }

                        if (IsItemInLibrary(item, libraryFolder))
                        {
                            this.Logger.LogDebug($"Item '{item.Name}' belongs to library '{library.Name}'");
                            return libraryFolder;
                        }
                        else
                        {
                            this.Logger.LogDebug($"Item '{item.Name}' does not belong to library '{library.Name}'");
                        }
                    }
                    else
                    {
                        this.Logger.LogDebug($"Library '{library.Name}' is not a movie library (type: {library.CollectionType}), skipping");
                    }
                }

                this.Logger.LogWarning($"Could not find a movie library containing item '{item.Name}'");
            }
            catch (System.Exception ex)
            {
                this.Logger.LogError($"Error getting library for item {item.Name}: {ex.Message}");
                this.Logger.LogError($"Stack trace: {ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// Checks if an item belongs to a specific library using Jellyfin's internal item hierarchy.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <param name="libraryFolder">The library folder to check against</param>
        /// <returns>True if the item is in the library, false otherwise</returns>
        private bool IsItemInLibrary(BaseItem item, BaseItem libraryFolder)
        {
            this.Logger.LogDebug($"Checking if item '{item.Name}' (ID: {item.Id}) is in library folder '{libraryFolder.Name}' (ID: {libraryFolder.Id})");
            
            try
            {
                var query = new InternalItemsQuery();
                query.AncestorIds = new System.Guid[] { libraryFolder.Id };
                var libraryItems = Plugin.LibraryManager.GetItemList(query);
                
                bool isInLibrary = libraryItems.Any(libItem => libItem.Id == item.Id);
                this.Logger.LogDebug($"Item '{item.Name}' found in library '{libraryFolder.Name}': {isInLibrary} (Library contains {libraryItems.Count} items)");
                return isInLibrary;
            }
            catch (System.Exception ex)
            {
                this.Logger.LogError($"Error checking if item '{item.Name}' is in library '{libraryFolder.Name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all intro files. Not implemented in this plugin.
        /// </summary>
        /// <returns>Empty collection</returns>
        public IEnumerable<string> GetAllIntroFiles()
        {
            this.Logger.LogDebug("GetAllIntroFiles called - not implemented");
            return Enumerable.Empty<string>();
        }
    }
}
