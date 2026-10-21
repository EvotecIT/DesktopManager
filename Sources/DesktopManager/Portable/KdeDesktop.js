// request is serialized as JSON by the owning adapter, never interpolated as code.
var selected = desktopsForActivity(currentActivity()).filter(function (desktop) {
    return request.displayId === null || ('screen:' + desktop.screen) === request.displayId;
});
if (!selected.length) { throw new Error('No desktop matches the requested current-activity screen.'); }
if (request.action === 'read') {
    var desktop = selected[0];
    if (desktop.wallpaperPlugin !== 'org.kde.image') {
        print(JSON.stringify({supported: false, message: 'The current KDE wallpaper plugin is not a static image.'}));
    } else {
        desktop.currentConfigGroup = ['Wallpaper', 'org.kde.image', 'General'];
        print(JSON.stringify({supported: true, path: desktop.readConfig('Image', ''), fillMode: desktop.readConfig('FillMode', 2)}));
    }
} else if (request.action === 'apply') {
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
