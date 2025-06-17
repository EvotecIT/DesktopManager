using System.Drawing;
using System.Drawing.Imaging;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Gets a screenshot of the desktop.</summary>
/// <para type="synopsis">Captures a screenshot of the entire desktop.</para>
/// <para type="description">Captures the current desktop image. When a path is provided the image is saved as PNG; otherwise a Bitmap object is returned.</para>
[Cmdlet(VerbsCommon.Get, "DesktopScreenshot")]
public sealed class CmdletGetDesktopScreenshot : PSCmdlet {
    /// <summary>
    /// <para type="description">Optional path to save the screenshot as a PNG file.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 0)]
    public string Path;

    /// <summary>
    /// <para type="description">Index of the monitor to capture. Defaults to the entire virtual screen.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public int? Monitor;

    /// <summary>
    /// <para type="description">Region to capture in screen coordinates.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public Rectangle? Region;

    /// <summary>
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        Bitmap bitmap = Region != null
            ? ScreenshotService.CaptureRegion(Region.Value)
            : Monitor != null
                ? ScreenshotService.CaptureMonitor(Monitor.Value)
                : ScreenshotService.CaptureScreen();

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Path))) {
            bitmap.Save(Path, ImageFormat.Png);
            bitmap.Dispose();
            WriteObject(Path);
        } else {
            WriteObject(bitmap);
        }
    }
}
