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
    if (!options || options.isNil()) { return null; }
    var scaling = options.objectForKey($.NSWorkspaceDesktopImageScalingKey);
    var clipping = options.objectForKey($.NSWorkspaceDesktopImageAllowClippingKey);
    if (!scaling || scaling.isNil()) { return null; }
    var scale = Number(ObjC.unwrap(scaling));
    if (scale === 2) { return 0; }
    if (scale === 1) { return 2; }
    return clipping && !clipping.isNil() && Boolean(ObjC.unwrap(clipping)) ? 4 : 3;
}

function backgroundColor(options) {
    if (!options || options.isNil()) { return null; }
    var color = options.objectForKey($.NSWorkspaceDesktopImageFillColorKey);
    if (!color || color.isNil()) { return null; }
    var rgb = color.colorUsingColorSpace($.NSColorSpace.sRGBColorSpace);
    if (!rgb || rgb.isNil()) { return null; }
    function hex(value) { return ('0' + Math.round(Math.max(0, Math.min(1, Number(value))) * 255).toString(16)).slice(-2); }
    return '#' + hex(rgb.redComponent) + hex(rgb.greenComponent) + hex(rgb.blueComponent);
}

function existingLocalImageUrl(url) {
    return url && !url.isNil() && url.isFileURL && $.NSFileManager.defaultManager.fileExistsAtPath(url.path);
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
    if (action === 'capture') {
        var captured = [];
        for (var captureIndex = 0; captureIndex < selected.length; captureIndex++) {
            var screen = selected[captureIndex];
            var originalUrl = $.NSWorkspace.sharedWorkspace.desktopImageURLForScreen(screen);
            var originalOptions = $.NSWorkspace.sharedWorkspace.desktopImageOptionsForScreen(screen);
            if (!existingLocalImageUrl(originalUrl) || !originalOptions || originalOptions.isNil()) {
                throw new Error('The active screen does not expose a restorable native image URL and options.');
            }
            // AppKit's URL/options omit aerial/dynamic selection and all-Spaces state.
            // Native probes showed that replaying an existing HEIC URL is not faithful recovery.
            var originalPath = String(ObjC.unwrap(originalUrl.path));
            if (!/\.(png|jpe?g|bmp|tiff?)$/i.test(originalPath) || position(originalOptions) === null) {
                return JSON.stringify({unsupported: 'Only static PNG/JPEG/BMP/TIFF backgrounds with explicit AppKit fit are eligible for URL/options checkpoints. HEIC, aerial/dynamic and unknown-fit selections are unsupported.'});
            }
            var archive = $.NSKeyedArchiver.archivedDataWithRootObject(originalOptions);
            captured.push({id: displayId(screen), url: ObjC.unwrap(originalUrl.absoluteString),
                options: ObjC.unwrap(archive.base64EncodedStringWithOptions(0))});
        }
        return JSON.stringify(captured);
    }
    if (action === 'restore') {
        // The opaque archive came from this service's own in-memory checkpoint; no file or external archive is loaded.
        var checkpoint = JSON.parse(args[2]);
        if (checkpoint.length !== selected.length) { throw new Error('The macOS screen topology changed; no images were restored.'); }
        var restore = checkpoint.map(function (entry) {
            var matches = selected.filter(function (screen) { return displayId(screen) === entry.id; });
            if (matches.length !== 1) { throw new Error('An original macOS screen is unavailable; no images were restored.'); }
            var nativeUrl = $.NSURL.URLWithString($(entry.url));
            if (!existingLocalImageUrl(nativeUrl)) {
                throw new Error('An original macOS image is unavailable; no images were restored.');
            }
            var data = $.NSData.alloc.initWithBase64EncodedStringOptions($(entry.options), 0);
            var nativeOptions = $.NSKeyedUnarchiver.unarchiveObjectWithData(data);
            if (!nativeOptions || nativeOptions.isNil()) { throw new Error('The native macOS image options could not be restored.'); }
            return {screen: matches[0], url: nativeUrl, options: nativeOptions};
        });
        var failures = [];
        restore.forEach(function (entry) {
            var error = Ref();
            if (!$.NSWorkspace.sharedWorkspace.setDesktopImageURLForScreenOptionsError(entry.url, entry.screen, entry.options, error)) {
                failures.push(error[0] ? ObjC.unwrap(error[0].localizedDescription) : 'AppKit rejected a restored screen.');
            }
        });
        if (failures.length) { throw new Error('macOS restoration was incomplete: ' + failures.join('; ')); }
        return 'true';
    }
    if (action === 'read') {
        var screen = selected[0];
        var url = $.NSWorkspace.sharedWorkspace.desktopImageURLForScreen(screen);
        var options = $.NSWorkspace.sharedWorkspace.desktopImageOptionsForScreen(screen);
        return JSON.stringify({path: url && !url.isNil() ? ObjC.unwrap(url.path) : null, position: position(options), color: backgroundColor(options)});
    }
    if (action === 'apply') {
        var fit = Number(args[3]);
        var scaling = fit === 0 ? 2 : (fit === 2 ? 1 : 3);
        var url = $.NSURL.fileURLWithPath($(args[2]));
        selected.forEach(function (screen) {
            // Preserve per-screen options such as the letterbox color; only fit is requested here.
            var original = $.NSWorkspace.sharedWorkspace.desktopImageOptionsForScreen(screen);
            var settings = original && !original.isNil() ? original.mutableCopy : $.NSMutableDictionary.dictionary;
            settings.setObjectForKey($(scaling), $.NSWorkspaceDesktopImageScalingKey);
            settings.setObjectForKey($(fit === 4 || fit === 2), $.NSWorkspaceDesktopImageAllowClippingKey);
            var error = Ref();
            if (!$.NSWorkspace.sharedWorkspace.setDesktopImageURLForScreenOptionsError(url, screen, settings, error)) {
                throw new Error(error[0] ? ObjC.unwrap(error[0].localizedDescription) : 'AppKit could not set the desktop image.');
            }
        });
        return 'true';
    }
    throw new Error('Unknown desktop operation.');
}
