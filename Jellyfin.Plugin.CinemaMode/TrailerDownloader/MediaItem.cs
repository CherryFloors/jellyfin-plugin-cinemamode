using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaMode.TrailerDownloader;

public class MediaItem
{
    public string Id { get; internal set; }
    public string Title { get; internal set; }
    public IEnumerable<string> Files { get; internal set; }
    public string TmdbId { get; internal set; }
}