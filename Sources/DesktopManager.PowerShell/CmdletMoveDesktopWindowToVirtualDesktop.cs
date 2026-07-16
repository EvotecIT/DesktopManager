namespace DesktopManager.PowerShell;

/// <summary>Moves a top-level window to a known virtual desktop.</summary>
[Cmdlet(VerbsCommon.Move, "DesktopWindowToVirtualDesktop", SupportsShouldProcess = true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletMoveDesktopWindowToVirtualDesktop : PSCmdlet {
    /// <summary><para type="description">The top-level window handle.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public IntPtr Handle;

    /// <summary><para type="description">A desktop identifier obtained from a top-level window.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    public Guid DesktopId;

    /// <summary>Moves the window.</summary>
    protected override void BeginProcessing() {
        if (ShouldProcess($"Window 0x{Handle.ToInt64():X}", $"Move to virtual desktop {DesktopId}")) {
            using var service = new VirtualDesktopService();
            service.MoveWindowToDesktop(Handle, DesktopId);
        }
    }
}
