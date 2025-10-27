namespace Jellyfin.Plugin.CinemaMode.TrailerDownloader;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

public class YoutubeDownloader
{
    private readonly ILogger<YoutubeDownloader> _logger;
    public YoutubeDownloader(ILogger<YoutubeDownloader> logger)
    {
        _logger = logger;
    }

    public async Task Init()
    {
        _logger.LogDebug("Downloading binaries...");

        await YoutubeDLSharp.Utils.DownloadBinaries();

        _logger.LogDebug("Binaries downloaded successfully.");

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            _logger.LogDebug("Setting executable permissions for yt-dlp on Linux...");

            // Fix a bug in YoutubeDlSharp here
            File.SetUnixFileMode("yt-dlp", UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                      UnixFileMode.GroupExecute | UnixFileMode.GroupRead |
                                      UnixFileMode.OtherExecute | UnixFileMode.OtherRead);

            _logger.LogDebug("Executable permissions set for yt-dlp on Linux.");
        }
    }

    public async Task<bool> DownloadAsync(string url, string title, string outputFolder)
    {
        var youtubeDl = new YoutubeDLSharp.YoutubeDL
        {
            // YoutubeDLPath = "yt-dlp",
            // FFmpegPath = "ffmpeg",
            OutputFolder = outputFolder
        };

        _logger.LogDebug("Starting download for '{Title}' from {Url}", title, url);
        var options = new YoutubeDLSharp.Options.OptionSet() { Output = $"{youtubeDl.OutputFolder}/{title}.%(ext)s" };

        var result = await youtubeDl.RunVideoDownload(url, format: "mp4", overrideOptions: options);

        if (!result.Success)
        {
            _logger.LogError("Youtube download failed for {Title}: {Error}", title, result.ErrorOutput);

            if (result.ErrorOutput.Contains("/usr/bin/env: ‘python3’: No such file or directory"))
            {
                _logger.LogCritical("Python3 not found on the machine, cannot download trailers!");
                throw new("Python3 not found on the machine, cannot download trailers!");
            }

            return false;
        }

        _logger.LogDebug("Download succeeded: {Title}", title);
        return true;
    }
}