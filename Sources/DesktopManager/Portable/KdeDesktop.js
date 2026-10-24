// request is serialized as JSON by the owning adapter, never interpolated as code.
var activity = currentActivity();
if (request.expectedScopeId !== undefined && request.expectedScopeId !== activity) {
    throw new Error('The current KDE activity changed; rediscover wallpaper targets before applying an image.');
}
var selected = desktopsForActivity(activity).filter(function (desktop) {
    return desktop.screen >= 0 && (request.displayId === null || ('screen:' + desktop.screen) === request.displayId);
});
if (!selected.length) { throw new Error('No desktop matches the requested current-activity screen.'); }
function imageConfig(desktop) {
    // Plasma's public ConfigFile API shares the live corona configuration for its standard file.
    // Compare live key presence before using it, so a custom shell/config is never silently substituted.
    var containments = ConfigFile('plasma-org.kde.plasma.desktop-appletsrc', 'Containments');
    if (containments.groupList.indexOf(String(desktop.id)) < 0) {
        throw new Error('The KDE containment is not present in the standard live shell configuration.');
    }
    var containment = ConfigFile(containments, String(desktop.id));
    var wallpaper = ConfigFile(containment, 'Wallpaper');
    var plugin = ConfigFile(wallpaper, 'org.kde.image');
    var general = ConfigFile(plugin, 'General');
    desktop.currentConfigGroup = ['Wallpaper', 'org.kde.image', 'General'];
    ['Image', 'FillMode'].forEach(function (key) {
        if ((general.keyList.indexOf(key) >= 0) !== (desktop.configKeys.indexOf(key) >= 0)) {
            throw new Error('The KDE shell does not expose matching live wallpaper configuration.');
        }
        if (desktop.configKeys.indexOf(key) >= 0 && String(general.readEntry(key)) !== String(desktop.readConfig(key, ''))) {
            throw new Error('The KDE shell configuration differs from the active wallpaper.');
        }
    });
    return general;
}
if (request.action === 'capture') {
    print(JSON.stringify({activity: activity, desktops: selected.map(function (desktop) {
        imageConfig(desktop);
        return {id: desktop.id, screen: desktop.screen, plugin: desktop.wallpaperPlugin,
            imagePresent: desktop.configKeys.indexOf('Image') >= 0, image: desktop.readConfig('Image', ''),
            fillPresent: desktop.configKeys.indexOf('FillMode') >= 0, fillMode: desktop.readConfig('FillMode', 2)};
    })}));
} else if (request.action === 'restore') {
    var checkpoint = request.checkpoint;
    if (checkpoint.activity !== activity || checkpoint.desktops.length !== selected.length) {
        throw new Error('The KDE activity or screen topology changed; no settings were restored.');
    }
    var restore = checkpoint.desktops.map(function (entry) {
        var matches = selected.filter(function (desktop) { return desktop.id === entry.id && desktop.screen === entry.screen; });
        if (matches.length !== 1 || matches[0].locked) {
            throw new Error('An original KDE desktop is unavailable or locked; no settings were restored.');
        }
        return {desktop: matches[0], original: entry, config: imageConfig(matches[0])};
    });
    var failures = [];
    restore.forEach(function (item) {
        try {
            item.desktop.writeConfig('Image', item.original.image);
            item.desktop.writeConfig('FillMode', item.original.fillMode);
            if (!item.original.imagePresent) { item.config.deleteEntry('Image'); }
            if (!item.original.fillPresent) { item.config.deleteEntry('FillMode'); }
            item.desktop.wallpaperPlugin = item.original.plugin;
            item.desktop.reloadConfig();
        } catch (error) { failures.push(String(error)); }
    });
    if (failures.length) { throw new Error('KDE restoration was incomplete: ' + failures.join('; ')); }
    print('true');
} else if (request.action === 'targets') {
    print(JSON.stringify(selected.map(function (desktop) {
        return {displayId: 'screen:' + desktop.screen, activity: activity};
    })));
} else if (request.action === 'read') {
    var desktop = selected[0];
    if (desktop.wallpaperPlugin !== 'org.kde.image') {
        print(JSON.stringify({supported: false, message: 'The current KDE wallpaper plugin is not a static image.'}));
    } else {
        desktop.currentConfigGroup = ['Wallpaper', 'org.kde.image', 'General'];
        print(JSON.stringify({supported: true, path: desktop.readConfig('Image', ''), fillMode: desktop.readConfig('FillMode', 2),
            color: desktop.configKeys.indexOf('Color') >= 0 ? String(desktop.readConfig('Color', '')) : null,
            blur: desktop.readConfig('Blur', false)}));
    }
} else if (request.action === 'apply') {
    if (selected.some(function (desktop) { return desktop.locked; })) {
        throw new Error('A requested KDE desktop is locked; no wallpaper was changed.');
    }
    selected.forEach(function (desktop) {
        desktop.wallpaperPlugin = 'org.kde.image';
        desktop.currentConfigGroup = ['Wallpaper', 'org.kde.image', 'General'];
        desktop.writeConfig('Image', request.uri);
        desktop.writeConfig('FillMode', request.fillMode);
    });
    print('true');
} else {
    throw new Error('Unknown desktop operation.');
}
