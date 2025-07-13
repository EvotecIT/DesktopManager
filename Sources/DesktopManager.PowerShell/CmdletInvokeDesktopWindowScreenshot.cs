using System.Drawing;
using System.Drawing.Imaging;
using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Takes a screenshot of a window or control.</summary>
/// <para type="synopsis">Captures a screenshot of a window or control.</para>
/// <example>
///   <code>$wnd = Get-DesktopWindow -Name "*Notepad*" | Select-Object -First 1
/// Invoke-DesktopWindowScreenshot -Window $wnd -Path "window.png"</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopWindowScreenshot")]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopWindowScreenshot : PSCmdlet {
    /// <summary>
    /// <para type="description">Window to capture.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Window")]
    public WindowInfo Window { get; set; } = null!;

    /// <summary>
    /// <para type="description">Control to capture.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Control")]
    public WindowControlInfo Control { get; set; } = null!;

    /// <summary>
    /// <para type="description">Optional path to save the PNG image.</para>
    /// </summary>
    [Parameter(Mandatory = false, Position = 1)]
    public string Path { get; set; }

    /// <inheritdoc/>
    protected override void BeginProcessing() {
        Bitmap bmp = ParameterSetName == "Window"
            ? ScreenshotService.CaptureWindow(Window.Handle)
            : ScreenshotService.CaptureControl(Control.Handle);

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Path))) {
            bmp.Save(Path, ImageFormat.Png);
            bmp.Dispose();
            WriteObject(Path);
        } else {
            WriteObject(bmp);
        }
    }
}
