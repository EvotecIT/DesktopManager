namespace DesktopManager;

/// <summary>Routes native wallpaper work through one COM apartment, including returned Shell items.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class DesktopManagerWrapper {
    private static readonly Guid ClassId = new("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");

    internal static IDesktopManager Create() => new ApartmentDesktopManager();

    /// <summary>Keeps an entire Shell operation in the wallpaper apartment; injected managers execute directly.</summary>
    internal static T Invoke<T>(IDesktopManager manager, Func<T> operation) =>
        manager is ApartmentDesktopManager ? WallpaperComApartment.Invoke(operation) : operation();

    internal static void Invoke(IDesktopManager manager, Action operation) =>
        Invoke(manager, () => { operation(); return true; });

    private sealed class ApartmentDesktopManager : IDesktopManager {
        // Like the legacy RCW, this wrapper owns one native instance for its lifetime.
        // Activation and every call occur on the MTA; the generated wrapper's finalizer
        // can release it from the finalizer MTA when this managed proxy is collected.
        private readonly Lazy<IDesktopManager> _instance = new(() => {
            object instance = NativeComActivation.CreateInstance(ClassId);
            try { return (IDesktopManager)instance; }
            catch { NativeComActivation.Release(instance); throw; }
        });

        private T Call<T>(Func<IDesktopManager, T> operation) => WallpaperComApartment.Invoke(() => operation(_instance.Value));

        private void Call(Action<IDesktopManager> operation) => Call(manager => { operation(manager); return true; });

        public void SetWallpaper(string monitorId, string wallpaper) => Call(manager => manager.SetWallpaper(monitorId, wallpaper));
        public string GetWallpaper(string monitorId) => Call(manager => manager.GetWallpaper(monitorId));
        public string GetMonitorDevicePathAt(uint monitorIndex) => Call(manager => manager.GetMonitorDevicePathAt(monitorIndex));
        public uint GetMonitorDevicePathCount() => Call(manager => manager.GetMonitorDevicePathCount());
        public RECT GetMonitorBounds(string monitorId) => Call(manager => manager.GetMonitorBounds(monitorId));
        public void SetBackgroundColor(uint color) => Call(manager => manager.SetBackgroundColor(color));
        public uint GetBackgroundColor() => Call(manager => manager.GetBackgroundColor());
        public void SetPosition(DesktopWallpaperPosition position) => Call(manager => manager.SetPosition(position));
        public DesktopWallpaperPosition GetPosition() => Call(manager => manager.GetPosition());
        public void SetSlideshow(IntPtr items) => Call(manager => manager.SetSlideshow(items));
        public uint GetSlideshow(out IntPtr items) {
            IntPtr result = IntPtr.Zero;
            var status = Call(manager => manager.GetSlideshow(out result));
            items = result;
            return status;
        }
        public void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) =>
            Call(manager => manager.SetSlideshowOptions(options, slideshowTick));
        public uint GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick) {
            DesktopSlideshowOptions currentOptions = default;
            uint currentTick = 0;
            var status = Call(manager => manager.GetSlideshowOptions(out currentOptions, out currentTick));
            options = currentOptions;
            slideshowTick = currentTick;
            return status;
        }
        public void AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) => Call(manager => manager.AdvanceSlideshow(monitorId, direction));
        public uint GetStatus(out DesktopSlideshowState state) {
            DesktopSlideshowState current = default;
            var status = Call(manager => manager.GetStatus(out current));
            state = current;
            return status;
        }
        public void Enable(bool enable) => Call(manager => manager.Enable(enable));
    }
}
