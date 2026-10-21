// JavaScript for Automation is a system-provided host for public AppKit APIs.
// It deliberately does not automate Finder or edit Apple's private wallpaper stores.
ObjC.import('AppKit');
ObjC.import('CoreGraphics');
ObjC.bindFunction('CGDisplayPixelsWide', ['size_t', ['uint32_t']]);
ObjC.bindFunction('CGDisplayPixelsHigh', ['size_t', ['uint32_t']]);

function displayId(screen) {
    return String(ObjC.unwrap(screen.deviceDescription.objectForKey('NSScreenNumber')));
}

function screens() {
    var values = [];
    var nativeScreens = $.NSScreen.screens;
    for (var index = 0; index < nativeScreens.count; index++) {
        values.push(nativeScreens.objectAtIndex(index));
    }
    return values;
}

function findScreens(id) {
    var values = screens();
    return id ? values.filter(function (screen) { return displayId(screen) === id; }) : values;
}

function position(options) {
    var scaling = options.objectForKey($.NSWorkspaceDesktopImageScalingKey);
    var clipping = options.objectForKey($.NSWorkspaceDesktopImageAllowClippingKey);
    if (!scaling || scaling.isNil()) { return null; }
    var scale = Number(ObjC.unwrap(scaling));
    if (scale === 2) { return 0; }
    if (scale === 1) { return 2; }
    return clipping && !clipping.isNil() && Boolean(ObjC.unwrap(clipping)) ? 4 : 3;
}

function run(args) {
    var action = args[0];
    var selected = findScreens(args[1] || null);
    if (action === 'displays') {
        return JSON.stringify(selected.map(function (screen, index) {
            var frame = screen.frame;
            var id = displayId(screen);
            return {
                id: id,
                index: index,
                name: ObjC.unwrap(screen.localizedName) || ('Display ' + id),
                primary: index === 0,
                left: Math.round(frame.origin.x),
                top: Math.round(frame.origin.y),
                width: Number($.CGDisplayPixelsWide(Number(id))),
                height: Number($.CGDisplayPixelsHigh(Number(id))),
                scale: Number(screen.backingScaleFactor)
            };
        }));
    }
    if (!selected.length) { throw new Error('The requested macOS display is not available.'); }
    if (action === 'read') {
        var screen = selected[0];
        var url = $.NSWorkspace.sharedWorkspace.desktopImageURLForScreen(screen);
        var options = $.NSWorkspace.sharedWorkspace.desktopImageOptionsForScreen(screen);
        return JSON.stringify({path: url && !url.isNil() ? ObjC.unwrap(url.path) : null, position: position(options)});
    }
    if (action === 'apply') {
        var fit = Number(args[3]);
        var scaling = fit === 0 ? 2 : (fit === 2 ? 1 : 3);
        var settings = $.NSMutableDictionary.dictionary;
        settings.setObjectForKey($(scaling), $.NSWorkspaceDesktopImageScalingKey);
        settings.setObjectForKey($(fit === 4 || fit === 2), $.NSWorkspaceDesktopImageAllowClippingKey);
        var url = $.NSURL.fileURLWithPath($(args[2]));
        selected.forEach(function (screen) {
            var error = Ref();
            if (!$.NSWorkspace.sharedWorkspace.setDesktopImageURLForScreenOptionsError(url, screen, settings, error)) {
                throw new Error(error[0] ? ObjC.unwrap(error[0].localizedDescription) : 'AppKit could not set the desktop image.');
            }
        });
        return 'true';
    }
    throw new Error('Unknown desktop operation.');
}
