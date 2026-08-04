// Mocks the Jellyfin globals that config.js depends on.
// Calls the local dev server instead of a real Jellyfin instance.

var ApiClient = {
    getVirtualFolders: function () {
        console.log('[cinema-mode] ApiClient.getVirtualFolders');
        return fetch('/api/virtualfolders').then(function (r) { return r.json(); });
    },
    getPluginConfiguration: function (_pluginId) {
        console.log('[cinema-mode] ApiClient.getPluginConfiguration');
        return fetch('/api/pluginconfiguration').then(function (r) { return r.json(); });
    },
    updatePluginConfiguration: function (_pluginId, config) {
        console.log('[cinema-mode] ApiClient.updatePluginConfiguration', config);
        return fetch('/api/pluginconfiguration', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config),
        }).then(function (r) { return r.json(); });
    },
};

var Dashboard = {
    showLoadingMsg: function () {},
    hideLoadingMsg: function () {},
    processPluginConfigurationUpdateResult: function () {
        var banner = document.getElementById('dev-save-banner');
        if (!banner) return;
        banner.style.display = 'inline';
        setTimeout(function () { banner.style.display = 'none'; }, 2000);
    },
};
