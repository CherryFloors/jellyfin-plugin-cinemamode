using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TMDbLib.Client;
using TMDbLib.Objects.Search;

namespace Jellyfin.Plugin.CinemaMode.TrailerDownloader;

public class TrailerDownloader
{
    private readonly ILogger<TrailerDownloader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TMDbClient _tmdbClient;
    private readonly YoutubeDownloader _youtubeDownloader;
    private readonly string[] _libraryIds;
    private readonly string _downloadTempFolder;
    private readonly ILibraryManager _libraryManager;
    public TrailerDownloader(
        string[] libraryIds,
        string downloadTempFolder,
        string tmdbApiKey,
        ILibraryManager libraryManager,
        ILogger<TrailerDownloader> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _tmdbClient = new TMDbClient(tmdbApiKey);

        _youtubeDownloader = new YoutubeDownloader(_serviceProvider.GetService<ILogger<YoutubeDownloader>>());

        if (libraryIds.Contains("*") && libraryIds.Length > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(libraryIds), "LibraryIds must not be '*' and include ids at the same time.");
        }
        _libraryIds = libraryIds;

        _downloadTempFolder = downloadTempFolder ?? throw new ArgumentNullException(nameof(downloadTempFolder));
        _libraryManager = libraryManager;
    }

    public async Task<DownloadStats> RunAsync(
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        var downloadStats = new DownloadStats();

        _logger.LogDebug("Starting trailer download RunAsync...");

        _logger.LogDebug("Temp folder is: {Folder}", _downloadTempFolder);

        Directory.CreateDirectory(_downloadTempFolder);

        // Cleaning up temp folder
        _logger.LogDebug("Cleaning up temp folder");
        foreach (var file in Directory.GetFiles(_downloadTempFolder, "*-trailer.mp4"))
        {
            _logger.LogDebug("Deleting file {file}", file);
            File.Delete(file);
        }

        _logger.LogDebug("Initializing Youtube Downloader");
        await _youtubeDownloader.Init();

        List<MediaItem> jellyfinMovies = [];

        if (_libraryIds.Length == 0)
        {
            _logger.LogWarning("No library selected in configuration. Skipping.");

            // return empty stats and finish
            return downloadStats;
        }
        else if (_libraryIds.Length == 1 && _libraryIds.Contains("*"))
        {
            _logger.LogInformation("Fetching movies with TMDb ID from all libraries");
            // leaving guids as an empty array --> means ALL

            jellyfinMovies.AddRange(await GetItemsWithTmdbIdAsync(null));
        }
        else
        {
            var guids = _libraryIds.Select(id => Guid.ParseExact(id, "N"));
            _logger.LogInformation("Fetching movies with TMDb ID from selected libraries: {libs}", string.Join(',', guids));

            foreach (var g in guids)
            {
                jellyfinMovies.AddRange(await GetItemsWithTmdbIdAsync(g));
            }
        }

        _logger.LogInformation("Found {Count} movies with TMDb ID in library", jellyfinMovies.Count);

        int movieIdx = 0;

        downloadStats.Total = jellyfinMovies.Count;

        foreach (var movie in jellyfinMovies)
        {
            movieIdx++;
            progress?.Report(100.0 / jellyfinMovies.Count * movieIdx);

            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Trailer download cancelled.");
                return downloadStats;
            }

            _logger.LogInformation("Processing movie {Index}/{Count}: {Movie} [TMDbId: {Id}]",
                movieIdx, jellyfinMovies.Count,
                movie.Title,
                movie.TmdbId);

            if (movie.Files == null || !movie.Files.Any())
            {
                _logger.LogError("No files found for movie: {Movie}. Skipping.", movie.Title);
                downloadStats.Error++;
                continue;
            }
            if (!File.Exists(movie.Files.First()))
            {
                _logger.LogError("Movie file does not exist: {File}. Skipping.", movie.Files.First());
                downloadStats.Error++;
                continue;
            }

            string movieFolder = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(movie.Files.FirstOrDefault()));
            if (string.IsNullOrEmpty(movieFolder) || !Directory.Exists(movieFolder))
            {
                _logger.LogError("No existing movie folder found for {Movie}. Skipping.", movie.Title);
                downloadStats.Error++;
                continue;
            }

            var movieFilename = Path.GetFileNameWithoutExtension(movie.Files.First());

            _logger.LogDebug("File for movie: {Movie} [TMDbId: {Id}] - {file}",
                movie.Title,
                movie.TmdbId,
                movie.Files.First());

            string trailerFileName = $"{movieFilename}-trailer.mp4";

            string trailerFullPath = "";
            if (movieFolder.EndsWith(movieFilename))
            {
                // If the movie folder already ends with the movie filename, we assume it's already in a subfolder
                trailerFullPath = Path.Combine(movieFolder, trailerFileName);
            }
            else
            {
                // Otherwise, we create a subfolder for the movie
                trailerFullPath = Path.Combine(movieFolder, movieFilename, trailerFileName);
            }

            _logger.LogDebug("Trailer file name: {Trailerfile}", trailerFileName);
            _logger.LogDebug("Trailer full path: {Trailerfile}", trailerFullPath);

            if (File.Exists(trailerFullPath))
            {
                _logger.LogInformation("Trailer already exists, skipping.");
                downloadStats.Existing++;
                continue;
            }

            if (Directory.Exists(Path.GetDirectoryName(trailerFullPath)))
            {
                _logger.LogWarning("Subfolder already exists: {Folder}", Path.GetDirectoryName(trailerFullPath));
                // downloadStats.Error++;
                // continue;
            }

            var tmdbMovie = await _tmdbClient.GetMovieAsync(movie.TmdbId, TMDbLib.Objects.Movies.MovieMethods.Videos, cancellationToken);
            if (tmdbMovie?.Videos?.Results == null)
            {
                _logger.LogWarning("No trailer found in TMDb for movie: {Movie}. Skipping.", movie.Title);
                downloadStats.NoTrailer++;
                continue;
            }

            var trailerKey = tmdbMovie.Videos.Results
                .Where(v => v.Type == "Trailer" && v.Site == "YouTube")
                .Select(v => v.Key)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(trailerKey))
            {
                _logger.LogWarning("No trailer found in TMDb for movie: {Movie}. Skipping.", movie.Title);
                downloadStats.NoTrailer++;
                continue;
            }

            var trailerUrl = $"https://www.youtube.com/watch?v={trailerKey}";

            _logger.LogInformation("Downloading trailer for {Movie}: {Url}", movie.Title, trailerUrl);

            bool success = await _youtubeDownloader.DownloadAsync(
                trailerUrl,
                Path.GetFileNameWithoutExtension(trailerFileName),
                _downloadTempFolder);

            if (!success)
            {
                _logger.LogWarning("Download failed for {Movie}: {Url}. Skipping.", movie.Title, trailerUrl);
                downloadStats.DownloadError++;
                continue;
            }

            _logger.LogInformation("Trailer downloaded for {Movie}: {Url}", movie.Title, trailerUrl);

            string finalFolder = Path.GetDirectoryName(trailerFullPath)!;
            Directory.CreateDirectory(finalFolder);

            _logger.LogDebug("Moving trailer to {Path}", trailerFullPath);

            File.Move(Path.Combine(_downloadTempFolder, trailerFileName), trailerFullPath);
            _logger.LogDebug("Trailer moved to: {Path}", trailerFullPath);

            var movieFiles = Directory.GetFiles(movieFolder, $"{movieFilename}*.*");
            foreach (var file in movieFiles)
            {
                var dest = Path.Combine(finalFolder, Path.GetFileName(file));

                if (!File.Exists(dest) && file != dest)
                {
                    _logger.LogDebug("Moving movie file {File} to {dest}", file, dest);
                    File.Move(file, dest);
                }
            }

            _logger.LogInformation("Downloaded trailer for {Movie} to {Folder}", movie.Title, finalFolder);
            downloadStats.Downloaded++;
        }

        return downloadStats;
    }

    private Task<IEnumerable<MediaItem>> GetItemsWithTmdbIdAsync(Guid? parentId)
    {
        var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            EnableTotalRecordCount = true,
            IsVirtualItem = false,
            HasTmdbId = true,
            Recursive = true,
            IncludeItemTypes = [BaseItemKind.Movie],
        };

        // if there is a parentId passed, only fetch for specific parent
        if (parentId.HasValue)
        {
            query.ParentId = parentId.Value;
        }

        var itemsWithTmdb = _libraryManager.GetItemList(query).OfType<Movie>()
        .Where(item => item.HasProviderId(MetadataProvider.Tmdb))
        .Select(item => new MediaItem
        {
            Id = item.Id.ToString(),
            Title = item.Name,
            Files = item.GetMediaSources(false).Select(file => file.Path).ToList(),
            TmdbId = item.GetProviderId("Tmdb")
        });

        return Task.FromResult(itemsWithTmdb);
    }

    public class DownloadStats
    {
        public int Total { get; set; }
        public int Downloaded { get; set; }
        public int Error { get; set; }
        public int Existing { get; set; }
        public int NoTrailer { get; set; }
        public int DownloadError { get; set; }
    }
}
