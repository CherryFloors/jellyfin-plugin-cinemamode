<h1 align="center">Jellyfin Cinema Mode Plugin</h1>
<h3 align="center">Plugin for the <a href="https://jellyfin.org">Jellyfin Project</a></h3>

<p align="center">
<img alt="Plugin Banner" src="Jellyfin.Plugin.CinemaMode/Images/jellyfin-plugin-cinemamode.jpg"/>
</p>

Photo by <a href="https://unsplash.com/@grstocks?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText">GR Stocks</a> on <a href="https://unsplash.com/photos/q8P8YoR6erg?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText">Unsplash</a>
  
## About

Jellyfin Cinema Mode is a plugin built with .NET that allows users to enable Jellyfin's Cinema Mode functionality with 
local trailers and pre-rolls. The system admin can configure a set number of trailers to play before a movie. In
addition, pre-roll videos can be played before and after the block of trailers. All of this can be disabled at the user
level by turning off 'Cinema Mode' in the users Playback settings. Pro-tip: Skip any pre-roll or trailer by pressing the 
next button in the player. For more details see the [User Guide](#user-guide)

## Installation

To install this plugin, you will first need to add the repository in Jellyfin. Under 'Repositories' in the 'Plugin'
section of the dashboard, click the plus sign to add a new repository. You can give this repository any name you like
but be sure to paste the following URL in the 'Repository URL' field exactly as it appears:

```
https://raw.githubusercontent.com/CherryFloors/jellyfin-plugin-cinemamode/main/manifest.json
```

Now that you have added the repository, click on the Catalog section at the top of the screen. Under General, you
should see the Cinema Mode plugin. Click on the thumbnail and then click the 'install' button. Restart the server and
you're ready to configure.

More information about installing plugins can be found in the official docs
[here](https://jellyfin.org/docs/general/server/plugins/index.html#installing). A quick web search will also turn up
plenty of great tutorial videos for setting up Jellyfin, including how to install 3rd party plugins.

## User Guide

<p align="center">
<img alt="Plugin Diagram" src="https://github.com/CherryFloors/jellyfin-plugin-cinemamode/raw/main/Jellyfin.Plugin.CinemaMode/Images/cinema-mode-diagram.png"/>
</p>

The plugin is designed to enable Jellyfin's Cinema Mode with **local** content. Trailers stored alongside movies (as
described [here](https://jellyfin.org/docs/general/server/media/movies/#movie-extras)) and Pre-Roll videos [stored in
their own library](#pre-rolls) can be configured to play prior to movies in the order illustrated in the diagram
above. Each intro type can be configured/turned off using the plugins' configuration page. All can be disabled at the
user level by turning off Cinema Mode in the users Playback settings. Pro-tip: Skip any pre-roll or trailer by pressing
the next button in the player.

### Pre-Rolls

The plugin can be configured to play Pre-Roll content prior to movies. Pre-Rolls should be stored in their own library.
Jellyfin does not currently have a "Pre-Rolls" content type so the "Movies" content type must be used. The plugin only
supports playback of Pre-Rolls organized in libraries of this content type. The "Movies" content type allows for adding
metadata (Tags, Genre, Studios) to apply [Selection Rules](#selection-rules) based on the feature they will play before.

The plugin supports two types of Pre-Rolls, 'Trailer Pre-Roll' and 'Feature Pre-Roll'. The 'Trailer Pre-Roll' type is
meant to play prior to a block of trailers (think "Now playing on Jellyfin"). The 'Feature Pre-Roll' type is meant
to be the last intro played right before the movie (think "Now your feature presentation"). It is recommended that two
different libraries are used for storage, one for 'Trailer Pre-Rolls' and one for 'Feature Pre-Rolls'.

#### Pre-Roll Library

Select the library that contains the relevant Pre-Roll type. Setting this to "-" will disable the section.

#### Selection Rules

Selection Rules are used to filter Pre-Rolls based on metadata. For selection rules to work, Pre-Rolls must have
metadata. A description of the behavior of each Selection Rule can be found below. A '(t)' denotes the Selection Rule is
based on Tags. If there are no Selection Rules or none of the Selection Rules are successful, a random Pre-Roll will be
played.

- **Name (t)** - The plugin will search for a Pre-Roll that has a Tag matching the feature name. Example: You are
playing the movie "Back to the Future", the plugin will search for a Pre-Roll that has a Tag "Back to the Future".
- **Year (t)** - The plugin will search for a Pre-Roll that has a Tag matching the feature's release year. Example:
You are playing a movie released in 1981, the plugin will search for a Pre-Roll that has a Tag "1981".
- **Decade (t)** - The plugin will search for a Pre-Roll that has a Tag that matches the decade that the feature was
released. Example: You are playing a movie released in 1981, the plugin will search for a Pre-Roll that has a Tag
"1980s".
- **Seasonal (t)** - The plugin will search for a Pre-Roll that has an active [Seasonal Tag](#seasonal-tag-definitions). 
- **Genre** - The plugin will search for a Pre-Roll that has any of the same genres as the feature.
- **Studios** - The plugin will search for a Pre-Roll that has any of the same studios as the feature.
- **All Tags** - The default behavior when multiple tag rules are selected is to match **Any** of the tags. Selecting
this rule will require a Pre-Roll to contain all tags. Example: You have a Pre-Roll that you want to play before movies
released in the 1980s during Halloween ([see seasonal tag definitions](#seasonal-tag-definitions)). 

#### Ignore Out Of Season

When enabled, this setting will filter out any Pre-Rolls that match [Seasonal Tags](#seasonal-tag-definitions) not
currently in season. Enabled by default. 

**Example:** If you have a Seasonal Tag 'christmas' defined from 12/1 - 12/25, this setting will prevent that Pre-Roll
from being played any other time of the year.

#### Enforce Rating Limit

Pre-Roll's use the 'Movies' content type allowing them to be assigned ratings. This setting can be used if you have 
Pre-Roll's that may not be suitable for all audiences and will ensure the rating of the Pre-Roll does not exceed that of
the feature. Rating limits for Pre-Rolls are slightly different from trailers as Pre-Roll's that are not assigned
ratings are considered suitable for all audiences. Enabled by default.

### Trailers

The plugin will automatically find any trailers you have stored alongside the movies in your Jellyfin library. The
plugin does not support playback of remote trailers. For information on how to add local trailers to Jellyfin, follow
[this guide](https://jellyfin.org/docs/general/server/media/movies/#movie-extras). 

#### Number of Trailers

This setting will set the number of trailers to play. Setting this to '0' will disable the section. Set to '2' by
default.

#### Selection Rules

Selection Rules are used to filter trailers based on metadata. A description of the behavior of each Selection Rule can
be found below. If there are no Selection Rules or none of the Selection Rules are successful, random Trailers will be
played until the desired number of trailers has been reached.

- **Year** - Find trailers for movies released in the same year as the feature.
- **Decade** - Find trailers for movies released in the same decade as the feature.
- **Genre** - Find trailers for movies with any overlapping genres.
- **Recently Added** - Find trailers for movies added in the last month.
- **More Like This** - Find trailers for movies in the features' "More Like This" category.
- **Unplayed** - Find trailers for movies that have not yet been played by the user.

#### Consume Mode

When enabled, consume mode will play only one trailer from a selection rule before moving on to the next. When consume
mode is disabled, the plugin will move on to the next rule only if all trailers are played and the limit has not been
reached. Disabled by default.

#### Enforce Rating Limit

This ensures the rating of the trailer content does not exceed that of the feature (i.e. trailers for an "R" rated movie
will not be shown before a "PG-13" movie). This setting will cause any unrated content to have no trailers played prior
and not be shown as a trailer. In this case "unrated" refers to content where the "Parental Rating" field is blank. It
does not refer to content with an "NR" or "Not Rated" rating. Jellyfin considers content with a rating of "NR" as
exceeding an "R" rating. Enabled by default. 

### Seasonal Tag Definitions

Seasonal Tag Definitions can be used to link a Pre-Roll with a specific time of year using Tags. The plugin will only
use the Month and Day from the 'Start Date' and 'End Date' from the selection. The year will be ignored so definitions
do not need to be updated.

- **Tag** - The tag used to represent the season.
- **Start Date** - The day the season starts. Included as part of the season. Only the Month and Day are important, the
Year will be ignored by the plugin. 
- **End Date** - The day the season ends. Included as part of the season. Only the Month and Day are important, the Year
will be ignored by the plugin. 

### Troubleshooting

If the plugin is not providing intros check the following:
- "Cinema Mode" is enabled in your users playback settings.
- You have some trailers stored alongside your media following the naming conventions given
[here](https://jellyfin.org/docs/general/server/media/movies/#movie-extras).

## Build Process

1. Clone or download this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build plugin with following command

```sh
dotnet publish --configuration Release --output bin
```

4. Place the resulting file in the `plugins` folder

