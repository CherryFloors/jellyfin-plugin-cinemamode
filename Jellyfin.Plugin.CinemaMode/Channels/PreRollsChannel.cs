using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Tasks;
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaMode.Channels;

/// <summary>
/// Trailers Channel.
/// </summary>
public class PreRollsChannel : IChannel, IDisableMediaSourceDisplay, ISupportsMediaProbe, IScheduledTask
{
    private readonly ILogger<PreRollsChannel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreRollsChannel"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{PreRollsChannel}"/> interface..</param>
    /// <param name="channnelManager">Instance of the <see cref="channnelManager"/>.</param>
    public PreRollsChannel(ILogger<PreRollsChannel> logger)
    {
        _logger = logger;

    }

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    public string Name { get; } = "Pre-Rolls";

    /// <inheritdoc />
    public string Key => "Pre-Rolls Refresh";

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
            AutoRefreshLevels = 4,
        };
    }

    /// <inheritdoc />
    public bool IsEnabledFor(string userId)
    {
        return true;
    }

    /// <summary>
    /// Get types of trailers.
    /// </summary>
    /// <returns><see cref="ChannelItemResult"/> containing the types of trailers.</returns>
    private ChannelItemResult GetChannelTypes()
    {
        _logger.LogDebug("Get Channel Types");
        return new ChannelItemResult
        {
            Items = new List<ChannelItemInfo>
            {
                new ChannelItemInfo
                {
                    Id = Plugin.Instance.Configuration.FeaturePreRollsChannelName,
                    FolderType = ChannelFolderType.Container,
                    Name = Plugin.Instance.Configuration.FeaturePreRollsChannelName,
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video
                },
                new ChannelItemInfo
                {
                    Id = Plugin.Instance.Configuration.TrailerPreRollsChannelName,
                    FolderType = ChannelFolderType.Container,
                    Name = Plugin.Instance.Configuration.TrailerPreRollsChannelName,
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video
                }
            },
            TotalRecordCount = 2
        };
    }

    private ChannelItemResult GetChannelItemResult(string channelPath)
    {

        if (string.IsNullOrEmpty(channelPath))
        {
            return new ChannelItemResult();
        }

        try
        {
            string[] preRolls = System.IO.Directory.GetFiles(channelPath);
            var channelItems = new List<ChannelItemInfo>();
            foreach (var preRoll in preRolls)
            {
                // Create media source
                List<MediaSourceInfo> mediaSources = new List<MediaSourceInfo>();
                mediaSources.Add(
                    new MediaSourceInfo
                    {
                        Path = preRoll,
                        Name = System.IO.Path.GetFileNameWithoutExtension(preRoll),
                        IsRemote = false,
                        Protocol = MediaProtocol.File,
                        SupportsProbing = true,
                        EncoderProtocol = MediaProtocol.File,
                    }
                );

                // Add channel item
                channelItems.Add(
                    new ChannelItemInfo
                    {
                        Id = preRoll,
                        FolderType = ChannelFolderType.Container,
                        Name = System.IO.Path.GetFileNameWithoutExtension(preRoll),
                        Type = ChannelItemType.Media,
                        MediaType = ChannelMediaType.Video,
                        MediaSources = mediaSources

                    }
                );
            }

            return new ChannelItemResult
            {
                Items = channelItems
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, nameof(GetChannelItemResult));
            return new ChannelItemResult();
        }
    }

    /// <inheritdoc />
    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        try
        {
            ChannelItemResult result = null;
            // Initial entry
            if (string.IsNullOrEmpty(query.FolderId))
            {
                return await Task.Run(() => GetChannelTypes());
            }

            // Get trailer pre rollls
            if (query.FolderId.Equals(Plugin.Instance.Configuration.TrailerPreRollsChannelName, StringComparison.OrdinalIgnoreCase))
            {
                result = await Task.Run(() => GetChannelItemResult(Plugin.Instance.Configuration.TrailerPreRollsPath));
            }

            // Get feature pre rollls
            else if (query.FolderId.Equals(Plugin.Instance.Configuration.FeaturePreRollsChannelName, StringComparison.OrdinalIgnoreCase))
            {
                result = await Task.Run(() => GetChannelItemResult(Plugin.Instance.Configuration.FeaturePreRollsPath));
            }

            return result ?? new ChannelItemResult();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.ToString());
            return new ChannelItemResult();
        }
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
        yield return ImageType.Thumb;
    }

    // Add auto refresh once a day
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        try
        {
            // Get this channel
            var q = Plugin.LibraryManager.GetItemList(
                new InternalItemsQuery
                {
                    Name = Name,
                    IncludeItemTypes = new BaseItemKind[] { BaseItemKind.Channel }
                }
            );

            // Check we got just one channel back
            if (q.Count == 1)
            {
                var thisChannel = q[0] as Channel;
                _logger.LogInformation($"Refreshing channel: {thisChannel.Name}");

                var query = new InternalItemsQuery
                {
                    Parent = thisChannel,
                    EnableTotalRecordCount = false,
                    ChannelIds = new Guid[] { thisChannel.Id }
                };

                var result = await Plugin.ChannelManager.GetChannelItemsInternal(query, new SimpleProgress<double>(), cancellationToken).ConfigureAwait(false);

                foreach (var item in result.Items)
                {
                    if (item is Folder folder)
                    {
                        _logger.LogInformation($"Refreshing folder: {folder.Name}");
                        await Plugin.ChannelManager.GetChannelItemsInternal(
                            new InternalItemsQuery
                            {
                                Parent = folder,
                                EnableTotalRecordCount = false,
                                ChannelIds = new Guid[] { thisChannel.Id }
                            },
                            new SimpleProgress<double>(),
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                _logger.LogInformation($"Finished refreshing channel: {thisChannel.Name}");

            }
            else
            {
                _logger.LogInformation("Failed to find channel when attempting to refresh");
            }

        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        progress.Report(100);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // At startup
        yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };

        // Every so often
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
            MaxRuntimeTicks = TimeSpan.FromHours(4).Ticks
        };

    }

}
