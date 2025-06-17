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
    /// Begin processing the command.
    /// </summary>
    protected override void BeginProcessing() {
        using var bitmap = ScreenshotService.CaptureScreen();
        if (MyInvocation.BoundParameters.ContainsKey(nameof(Path))) {
            bitmap.Save(Path, ImageFormat.Png);
            WriteObject(Path);
        } else {
            WriteObject(bitmap); // Bitmap disposed by PowerShell after pipeline
        }
    }
}
