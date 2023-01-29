using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaMode.Channels;

/// <summary>
/// Trailers Channel.
/// </summary>
public class TrailerPreRollsChannel : IChannel, IDisableMediaSourceDisplay, ISupportsMediaProbe
{
    private readonly ILogger<TrailerPreRollsChannel> _logger;
    // private readonly Channel _channnelManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrailerPreRollsChannel"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{TrailerPreRollsChannel}"/> interface..</param>
    /// <param name="channnelManager">Instance of the <see cref="channnelManager"/>.</param>
    public TrailerPreRollsChannel(ILogger<TrailerPreRollsChannel> logger)
    {
        _logger = logger;

    }

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    public string Name { get; } = Plugin.Instance.Configuration.TrailerPreRollsChannelName;

    /// <inheritdoc />
    // public string Key => "Feature Pre-Rolls Refresh";

    /// <summary>
    /// Gets the channel description.
    /// </summary>
    public string Description { get; } = Plugin.Instance.Description;

    /// <inheritdoc />
    public string Category => "Pre-Rolls";
    public SourceType SourceType => SourceType.Library;

    /// <inheritdoc />
    public string DataVersion => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string HomePageUrl => "https://jellyfin.org";

    /// <inheritdoc />
    public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

    /// <inheritdoc />
    public InternalChannelFeatures GetChannelFeatures()
    {
        _logger.LogDebug(nameof(GetChannelFeatures));
        return new InternalChannelFeatures
        {
            ContentTypes = new List<ChannelMediaContentType>
            {
                ChannelMediaContentType.MovieExtra
            },
            MediaTypes = new List<ChannelMediaType>
            {
                ChannelMediaType.Video
            },
            AutoRefreshLevels = 4
        };
    }

    /// <inheritdoc />
    public bool IsEnabledFor(string userId)
    {
        return true;
    }

    /// <inheritdoc />
    public List<MediaSourceInfo> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
    {
        List<MediaSourceInfo> result = new List<MediaSourceInfo>();
        MediaSourceInfo mediaSourceInfo = new MediaSourceInfo();
        mediaSourceInfo.Path = id;
        mediaSourceInfo.Name = System.IO.Path.GetFileNameWithoutExtension(id);
        mediaSourceInfo.IsRemote = false;
        mediaSourceInfo.Protocol = MediaProtocol.File;
        result.Add(mediaSourceInfo);
        return result;
    }

    /// <inheritdoc />
    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        ChannelItemResult channelItemResult = new ChannelItemResult();
        var Items = new List<ChannelItemInfo>();

        string[] preRolls = System.IO.Directory.GetFiles(Plugin.Instance.Configuration.TrailerPreRollsPath);
        foreach (string preRoll in preRolls)
        {
            Items.Add(
                new ChannelItemInfo
                {
                    Id = preRoll,
                    FolderType = ChannelFolderType.Container,
                    Name = System.IO.Path.GetFileNameWithoutExtension(preRoll),
                    Type = ChannelItemType.Media,
                    MediaType = ChannelMediaType.Video,
                    MediaSources = GetChannelItemMediaInfo(preRoll, cancellationToken)

                }
            );
        }
        channelItemResult.Items = Items;
        channelItemResult.TotalRecordCount = Items.Count;
        return channelItemResult;
    }

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    {
        DynamicImageResponse dynamicImageResponse = new DynamicImageResponse();
        return Task.FromResult(dynamicImageResponse);
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedChannelImages()
    {
        yield return ImageType.Screenshot;
    }
}
