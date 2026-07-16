using System.Threading;

namespace DesktopManager.PowerShell;

/// <summary>Prevents selected Windows idle power behaviors for a bounded duration.</summary>
[Cmdlet(VerbsLifecycle.Start, "DesktopKeepAwake")]
public sealed class CmdletStartDesktopKeepAwake : PSCmdlet {
    /// <summary><para type="description">How long the keep-awake lease should remain active.</para></summary>
    [Parameter(Mandatory = true, Position = 0)]
    public TimeSpan Duration;

    /// <summary><para type="description">Also prevents the display from turning off.</para></summary>
    [Parameter]
    public SwitchParameter Display;

    /// <summary><para type="description">Also requests away mode.</para></summary>
    [Parameter]
    public SwitchParameter AwayMode;

    /// <summary>Holds the bounded keep-awake lease.</summary>
    protected override void BeginProcessing() {
        if (Duration <= TimeSpan.Zero) {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentOutOfRangeException(nameof(Duration)),
                "InvalidDuration",
                ErrorCategory.InvalidArgument,
                Duration));
        }
        KeepAwakeOptions options = KeepAwakeOptions.System;
        if (Display) {
            options |= KeepAwakeOptions.Display;
        }
        if (AwayMode) {
            options |= KeepAwakeOptions.AwayMode;
        }
        using (new SystemPowerService().CreateKeepAwakeLease(options)) {
            Thread.Sleep(Duration);
        }
    }
}
