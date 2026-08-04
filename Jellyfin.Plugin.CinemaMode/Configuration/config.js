var pluginId = "5fcefe1b-df1f-4596-ac57-f2f939c294c5";
var mediaFolders = ApiClient.getVirtualFolders();

function removeListItem(item) {
    var p = item.parentElement;
    p.remove();
};

function clearList(id) {
    var listItems = document.getElementById(id);
    listItems.innerHTML = "";
}

function initPreRollRules(id, configPreRollRules)
{
    for (const rule of configPreRollRules) {
        var newRule = addPreRollRule(id).getElementsByClassName("listItemBody")[0];
        var _name = newRule.children[0].children[0];
        var _year = newRule.children[1].children[0];
        var _decade = newRule.children[2].children[0];
        var _seasonal = newRule.children[3].children[0];
        var _genre = newRule.children[4].children[0];
        var _studios = newRule.children[5].children[0];
        var _all_tags = newRule.children[6].children[0];

        _name.checked = rule.Name;
        _year.checked = rule.Year;
        _decade.checked = rule.Decade;
        _seasonal.checked = rule.Seasonal;
        _genre.checked = rule.Genre;
        _studios.checked = rule.Studios;
        _all_tags.checked = rule.AllTags;
    }
}

function initSeasonalTagDefs(configSeasonalTagDefs)
{
    for (const rule of configSeasonalTagDefs) {
        var newSeasonalTagDef = addSeasonalTagDef().getElementsByClassName("listItemBody")[0];
        var _tag = newSeasonalTagDef.children[0].children[0];
        var _start_date = newSeasonalTagDef.children[1].children[0];
        var _end_date = newSeasonalTagDef.children[2].children[0];

        _tag.value = rule.Tag;
        _start_date.value = rule.Start;
        _end_date.value = rule.End;
    }
}

function initTrailerRules(configTrailerSelectionRules)
{
    for (const rule of configTrailerSelectionRules) {
        var newTrailerSelectionRule = addTrailerRule('trailer-rules').getElementsByClassName("listItemBody")[0];
        var _year = newTrailerSelectionRule.children[0].children[0];
        var _decade = newTrailerSelectionRule.children[1].children[0];
        var _genre = newTrailerSelectionRule.children[2].children[0];
        var _recently_added = newTrailerSelectionRule.children[3].children[0];
        var _more_like_this = newTrailerSelectionRule.children[4].children[0];
        var _unplayed = newTrailerSelectionRule.children[5].children[0];

        _year.checked = rule.Year;
        _decade.checked = rule.Decade;
        _genre.checked = rule.Genre;
        _recently_added.checked = rule.RecentlyAdded;
        _more_like_this.checked = rule.MoreLikeThis;
        _unplayed.checked = rule.Unplayed;
    }
}

function getPreRollRules(id) {
    var rules = [];
    var ruleItems = document.getElementById(id).getElementsByClassName("listItemBody")
    for (const rule of ruleItems) {

        var _name = rule.children[0].children[0];
        var _year = rule.children[1].children[0];
        var _decade = rule.children[2].children[0];
        var _seasonal = rule.children[3].children[0];
        var _genre = rule.children[4].children[0];
        var _studios = rule.children[5].children[0];
        var _all_tags = rule.children[6].children[0];

        console.log(rule.children);
        console.log(_all_tags);
        rules.push(
            {
                "Name": _name.checked,
                "Year": _year.checked,
                "Decade": _decade.checked,
                "Seasonal": _seasonal.checked,
                "Genre": _genre.checked,
                "Studios": _studios.checked,
                "AllTags": _all_tags.checked,
            }
        );
    }
    return rules
}

function getSeasonalTagDefs() {
    var tagDefs = [];
    var seasonalTagDefs = document.getElementById('seasonal-tag-definitions').getElementsByClassName("listItemBody")
    for (const seasonalTagDef of seasonalTagDefs) {

        var _tag = seasonalTagDef.children[0].children[0];
        var _start_date = seasonalTagDef.children[1].children[0];
        var _end_date = seasonalTagDef.children[2].children[0];

        tagDefs.push(
            {
                "Tag": _tag.value,
                "Start": _start_date.value,
                "End": _end_date.value,
            }
        );
    }
    return tagDefs
}

function getTrailerRules() {
    var rules = [];
    var ruleItems = document.getElementById('trailer-rules').getElementsByClassName("listItemBody")
    for (const rule of ruleItems) {

        var _year = rule.children[0].children[0];
        var _decade = rule.children[1].children[0];
        var _genre = rule.children[2].children[0];
        var _recently_added = rule.children[3].children[0];
        var _more_like_this = rule.children[4].children[0];
        var _unplayed = rule.children[5].children[0];

        rules.push(
            {
                "Year": _year.checked,
                "Decade": _decade.checked,
                "Genre": _genre.checked,
                "RecentlyAdded": _recently_added.checked,
                "MoreLikeThis": _more_like_this.checked,
                "Unplayed": _unplayed.checked,
            }
        );
    }
    return rules
}

function preRollQueryCheckbox(label) {
    var checkbox = '<input type="checkbox" id="'.concat(label);
    checkbox += '" name="';
    checkbox += label;
    checkbox += '" />';
    checkbox += label;
    return '<label style="margin-right: 20px">'.concat(checkbox).concat('</label>');
};

function preRollQueryRuleFactory(prefix) {
    var opt = document.createElement('div');
    opt.setAttribute("class", "listItem listItem-border");

    var innerHTML = '<div class="listItemBody">'
    innerHTML += preRollQueryCheckbox("Name (t)");
    innerHTML += preRollQueryCheckbox("Year (t)");
    innerHTML += preRollQueryCheckbox("Decade (t)");
    innerHTML += preRollQueryCheckbox("Seasonal (t)");
    innerHTML += preRollQueryCheckbox("Genre");
    innerHTML += preRollQueryCheckbox("Studios");
    innerHTML += preRollQueryCheckbox("All Tags");
    innerHTML += '</div>';
    innerHTML += '<button onclick="removeListItem(this)" type="button" is="paper-icon-button-light" class="listItemButton btnRemovePath paper-icon-button-light" data-index="0">';
    innerHTML += '<span class="material-icons remove_circle" aria-hidden="true"></span>';
    innerHTML += '</button>';

    opt.innerHTML = innerHTML;
    return opt
};

function addPreRollRule(id) {
    var selector = document.getElementById(id);
    var newRule = preRollQueryRuleFactory(id);
    selector.appendChild(newRule);
    return newRule;
};

function trailerRuleFactory(prefix) {
    var opt = document.createElement('div');
    opt.setAttribute("class", "listItem listItem-border");

    var innerHTML = '<div class="listItemBody">'
    innerHTML += preRollQueryCheckbox("Year");
    innerHTML += preRollQueryCheckbox("Decade");
    innerHTML += preRollQueryCheckbox("Genre");
    innerHTML += preRollQueryCheckbox("Recently Added");
    innerHTML += preRollQueryCheckbox("More Like This");
    innerHTML += preRollQueryCheckbox("Unplayed");
    innerHTML += '</div>';
    innerHTML += '<button onclick="removeListItem(this)" type="button" is="paper-icon-button-light" class="listItemButton btnRemovePath paper-icon-button-light" data-index="0">';
    innerHTML += '<span class="material-icons remove_circle" aria-hidden="true"></span>';
    innerHTML += '</button>';

    opt.innerHTML = innerHTML;
    return opt
};

function addTrailerRule(id) {
    var selector = document.getElementById(id);
    var newRule = trailerRuleFactory(id);
    selector.appendChild(newRule);
    return newRule;
};

function seasonalTagDefFactory() {
    var opt = document.createElement('div');
    opt.setAttribute("class", "listItem listItem-border");

    var innerHTML = '<div class="listItemBody">'
    innerHTML += '<label style="margin-right: 20px">';
    innerHTML += 'Tag\n';
    innerHTML += '<input type="string" id="tag-input" name="tag-input"/>';
    innerHTML += '</label>';
    innerHTML += '<label style="margin-right: 20px">';
    innerHTML += 'Start Date\n';
    innerHTML += '<input type="date" id="start-date" name="start-date" />';
    innerHTML += '</label>';
    innerHTML += '<label style="margin-right: 20px">';
    innerHTML += 'End Date\n';
    innerHTML += '<input type="date" id="end-date" name="end-date" />';
    innerHTML += '</label>';
    innerHTML += '</div>';
    innerHTML += '<button onclick="removeListItem(this)" type="button" is="paper-icon-button-light" class="listItemButton btnRemovePath paper-icon-button-light" data-index="0">';
    innerHTML += '<span class="material-icons remove_circle" aria-hidden="true"></span>';
    innerHTML += '</button>';

    opt.innerHTML = innerHTML;
    return opt
}

function addSeasonalTagDef() {
    var selector = document.getElementById('seasonal-tag-definitions');
    var newTagDef = seasonalTagDefFactory();
    selector.appendChild(newTagDef);
    return newTagDef;
};

function selectElement(id, selection) {
    let element = document.getElementById(id);
    element.value = selection;
};

function initLibrarySelector(selectorID, optionID, optionValue) {
    var selector = document.getElementById(selectorID);
    mediaFolders.then((folderArray) => {
        var opt = document.createElement('option');
        opt.value = "-";
        opt.innerHTML = "-";
        selector.appendChild(opt);
        for (const folder of folderArray) {
            if (folder.CollectionType == "movies") {
                var opt = document.createElement('option');
                opt.value = folder.ItemId;
                opt.innerHTML = folder.Name;
                selector.appendChild(opt);
            };
        };
        selector.value = optionValue;
    });
};

$('.cinemaModeConfigurationPage').on('pageshow', function () {
    Dashboard.showLoadingMsg();
    clearList('trailerPreRollLibrarySelect');
    clearList('featurePreRollLibrarySelect');
    clearList('trailer-pre-roll-rules');
    clearList('feature-pre-roll-rules');
    clearList('trailer-rules');
    clearList('seasonal-tag-definitions');
    var page = this;
    ApiClient.getPluginConfiguration(pluginId).then(function (config) {
        $('#number-trailers', page).val(config.NumberOfTrailers);
        document.getElementById('enforce-rating-limit-trailers').checked = config.EnforceRatingLimitTrailers;
        initLibrarySelector('trailerPreRollLibrarySelect', config.TrailerPreRollsLibrary, config.TrailerPreRollsLibrary);
        initLibrarySelector('featurePreRollLibrarySelect', config.FeaturePreRollsLibrary, config.FeaturePreRollsLibrary);
        initPreRollRules('trailer-pre-roll-rules', config.TrailerPreRollsSelections);
        initPreRollRules('feature-pre-roll-rules', config.FeaturePreRollsSelections);

        document.getElementById('enforce-rating-limit-trailer-pre-rolls').checked = config.TrailerPreRollsRatingLimit;
        document.getElementById('enforce-rating-limit-feature-pre-rolls').checked = config.FeaturePreRollsRatingLimit;
        document.getElementById('ignore-out-of-season-trailer-pre-rolls').checked = config.TrailerPreRollsIgnoreOutOfSeason;
        document.getElementById('ignore-out-of-season-feature-pre-rolls').checked = config.FeaturePreRollsIgnoreOutOfSeason;
        document.getElementById('trailer-rules-consume-mode').checked = config.TrailerConsumeMode;

        initSeasonalTagDefs(config.SeasonalTagDefinitions);
        initTrailerRules(config.TrailerSelectionRules);
        Dashboard.hideLoadingMsg();
    });
});

$('.cinemaModeConfigurationPage').on('submit', function () {
    Dashboard.showLoadingMsg();

    var TrailerPreRollsLibrary = $('#trailerPreRollLibrarySelect').val();
    var FeaturePreRollsLibrary = $('#featurePreRollLibrarySelect').val();
    var NumberOfTrailers = $('#number-trailers').val();
    ApiClient.getPluginConfiguration(pluginId).then(function (config) {

        config.TrailerPreRollsLibrary = TrailerPreRollsLibrary;
        config.FeaturePreRollsLibrary = FeaturePreRollsLibrary;
        config.TrailerPreRollsSelections = getPreRollRules('trailer-pre-roll-rules');
        config.FeaturePreRollsSelections = getPreRollRules('feature-pre-roll-rules');
        config.TrailerPreRollsRatingLimit = document.getElementById('enforce-rating-limit-trailer-pre-rolls').checked;
        config.FeaturePreRollsRatingLimit = document.getElementById('enforce-rating-limit-feature-pre-rolls').checked;
        config.TrailerPreRollsIgnoreOutOfSeason = document.getElementById('ignore-out-of-season-trailer-pre-rolls').checked;
        config.FeaturePreRollsIgnoreOutOfSeason = document.getElementById('ignore-out-of-season-feature-pre-rolls').checked;
        config.SeasonalTagDefinitions = getSeasonalTagDefs();
        config.TrailerSelectionRules = getTrailerRules();
        config.NumberOfTrailers = parseInt(NumberOfTrailers);
        config.EnforceRatingLimitTrailers = document.getElementById('enforce-rating-limit-trailers').checked;
        config.TrailerConsumeMode = document.getElementById('trailer-rules-consume-mode').checked;

        ApiClient.updatePluginConfiguration(pluginId, config).then(Dashboard.processPluginConfigurationUpdateResult);
    });

    return false;
});
