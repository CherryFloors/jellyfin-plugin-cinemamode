using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using System.IO;

namespace Jellyfin.Plugin.CinemaMode;

public class TrailerDownloadScheduledTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrailerDownloadScheduledTask> _logger;
    private readonly string _tempPath;

    public string Name => "Download Trailers";
    public string Key => "TrailerDownloaderScheduledTask";
    public string Description => "Downloads trailers for movies in the library and puts them on the local folder.";
    public string Category => "Cinema Mode";

    public TrailerDownloadScheduledTask(
        ILibraryManager libraryManager,
        IServiceProvider serviceProvider,
        ILogger<TrailerDownloadScheduledTask> logger)
    {
        _libraryManager = libraryManager;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _tempPath = Path.Combine(Plugin.Instance.DataFolderPath, "downloads");

        _logger.LogDebug("Using temp path: {TempPath}", _tempPath);
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TrailerDownload Scheduled Task...");

        if (string.IsNullOrEmpty(Plugin.Instance.Configuration.TmdbApiKey))
        {
            _logger.LogError("TMDB API Key is not configured. Please set the TMDB API Key in the plugin configuration. Trailer download will not proceed.");
            return;
        }

        var trailerDownloader = new TrailerDownloader.TrailerDownloader(
            libraryIds: Plugin.Instance.Configuration.TrailerDownloadLibraries,
            downloadTempFolder: _tempPath,
            tmdbApiKey: Plugin.Instance.Configuration.TmdbApiKey,
            libraryManager: _libraryManager,
            logger: _serviceProvider.GetService<ILogger<TrailerDownloader.TrailerDownloader>>(),
            serviceProvider: _serviceProvider
            );

        var stats = await trailerDownloader.RunAsync(progress, cancellationToken);

        _logger.LogInformation("TrailerDownload Scheduled Task completed successfully.");
        _logger.LogInformation("   {s} movies with TMDb ID processed", stats.Total);
        _logger.LogInformation("   {s} movies with existing trailers", stats.Existing);
        _logger.LogInformation("   {s} trailers downloaded", stats.Downloaded);
        _logger.LogInformation("   {s} movies with no trailer", stats.NoTrailer);
        _logger.LogInformation("   {s} errors while downloading trailers", stats.DownloadError);
        _logger.LogInformation("   {s} errors", stats.Error);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(30)).Ticks, // Run daily at 7:30 AM
            }
        ];
    }
}
