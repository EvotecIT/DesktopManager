namespace DesktopManager.PowerShell;

/// <summary>Moves a top-level window to a known virtual desktop.</summary>
/// <para type="synopsis">Moves a top-level window to a known virtual desktop.</para>
/// <para type="description">The supported Windows API only moves windows that belong to the calling process; Windows refuses other processes' windows with access denied. Use -Experimental to move another process's window through the undocumented, version-gated Windows 11 shell contract.</para>
/// <example>
///   <para>Move another application's window to a desktop</para>
///   <code>Move-DesktopWindowToVirtualDesktop -Handle $window.Handle -DesktopId $desktop.Id -Experimental</code>
/// </example>
[Cmdlet(VerbsCommon.Move, "DesktopWindowToVirtualDesktop", SupportsShouldProcess = true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class CmdletMoveDesktopWindowToVirtualDesktop : PSCmdlet {
    /// <summary><para type="description">The top-level window handle.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public IntPtr Handle;

    /// <summary><para type="description">A desktop identifier obtained from a top-level window.</para></summary>
    [Parameter(Mandatory = true, Position = 1)]
    public Guid DesktopId;

    /// <summary><para type="description">When Windows refuses to move another process's window, move it through the experimental Windows 11 shell contract (builds 26100, 26200 and 26300). A window pinned to all desktops is unpinned; an application-wide pin is left alone and reported.</para></summary>
    [Parameter]
    public SwitchParameter Experimental;

    /// <summary>Moves the window.</summary>
    protected override void BeginProcessing() {
        if (!ShouldProcess($"Window 0x{Handle.ToInt64():X}", $"Move to virtual desktop {DesktopId}")) {
            return;
        }

        try {
            using var service = new VirtualDesktopService();
            service.MoveWindowToDesktop(Handle, DesktopId);
        } catch (UnauthorizedAccessException) when (Experimental) {
            // The experimental contract moves the window's application view, which works for any process's window.
            using var shell = new ExperimentalVirtualDesktopService();
            shell.RestoreWindow(new WindowPosition { Handle = Handle, VirtualDesktopId = DesktopId, PinMode = VirtualDesktopPinMode.None }, Handle);
        }
    }
}
