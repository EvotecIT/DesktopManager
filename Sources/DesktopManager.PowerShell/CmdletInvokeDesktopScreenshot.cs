using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Takes a screenshot of the desktop.</summary>
/// <para type="synopsis">Captures a screenshot of the desktop.</para>
/// <para type="description">Captures the current desktop image. When a path is provided the image is saved as PNG; otherwise a Bitmap object is returned. The screenshot can target a specific monitor or any region.</para>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopScreenshot")]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopScreenshot : PSCmdlet {
    /// <summary>
    /// <para type="description">Optional path to save the screenshot as a PNG file.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0)]
    public string Path;

    /// <summary>
    /// <para type="description">Index of the monitor to capture. Defaults to the entire virtual screen.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Index;

    /// <summary>
    /// <para type="description">Identifier of the monitor to capture.</para>
    /// </summary>
    [Alias("MonitorID")]
    [Parameter(Mandatory = false)]
    public string DeviceId;

    /// <summary>
    /// <para type="description">Name of the monitor to capture.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public string DeviceName;

    /// <summary>
    /// <para type="description">Capture the primary monitor only.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter PrimaryOnly;

    /// <summary>
    /// <para type="description">Left coordinate of the region to capture.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Left;

    /// <summary>
    /// <para type="description">Top coordinate of the region to capture.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Top;

    /// <summary>
    /// <para type="description">Width of the region to capture.</para>
    /// </summary>
    [Alias("Right")]
    [Parameter(Mandatory = false)]
    public int? Width;

    /// <summary>
    /// <para type="description">Height of the region to capture.</para>
    /// </summary>
    [Alias("Bottom")]
    [Parameter(Mandatory = false)]
    public int? Height;

    /// <summary>
    /// Captures a screenshot and writes it to the pipeline or to a file.
    /// </summary>
    protected override void BeginProcessing() {
        bool hasRegion = MyInvocation.BoundParameters.ContainsKey(nameof(Left)) &&
                          MyInvocation.BoundParameters.ContainsKey(nameof(Top)) &&
                          MyInvocation.BoundParameters.ContainsKey(nameof(Width)) &&
                          MyInvocation.BoundParameters.ContainsKey(nameof(Height));

        Bitmap bitmap;

        if (hasRegion) {
            int width = Width!.Value;
            int height = Height!.Value;
            bitmap = ScreenshotService.CaptureRegion(Left!.Value, Top!.Value, width, height);
        } else {
            Monitors monitors = new();
            bool? primaryOnly = MyInvocation.BoundParameters.ContainsKey(nameof(PrimaryOnly)) ? (bool?)PrimaryOnly : null;
            int? index = MyInvocation.BoundParameters.ContainsKey(nameof(Index)) ? (int?)Index : null;
            string deviceId = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceId)) ? DeviceId : null;
            string deviceName = MyInvocation.BoundParameters.ContainsKey(nameof(DeviceName)) ? DeviceName : null;

            var monitor = monitors.GetMonitors(primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName).FirstOrDefault();
            if (monitor != null) {
                var pos = monitor.GetMonitorPosition();
                bitmap = ScreenshotService.CaptureRegion(pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top);
            } else {
                bitmap = ScreenshotService.CaptureScreen();
            }
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Path))) {
            bitmap.Save(Path, ImageFormat.Png);
            bitmap.Dispose();
            WriteObject(Path);
        } else {
            WriteObject(bitmap);
        }
    }
}
