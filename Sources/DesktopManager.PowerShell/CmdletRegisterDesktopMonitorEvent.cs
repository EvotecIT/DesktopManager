#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Management.Automation;
using System.Timers;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Registers for desktop monitor change events.</summary>
[Cmdlet(VerbsLifecycle.Register, "DesktopMonitorEvent")]
[SupportedOSPlatform("windows")]
public sealed class CmdletRegisterDesktopMonitorEvent : PSCmdlet {
    /// <summary>The script block to run when the event is raised.</summary>
    [Parameter(Mandatory = false)]
    public ScriptBlock Action { get; set; }

    /// <summary>The duration to monitor before automatically unregistering.</summary>
    [Parameter(Mandatory = false)]
    public TimeSpan Duration { get; set; }

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        var watcher = new MonitorWatcher();
        var subscriber = Events.SubscribeEvent(watcher, nameof(MonitorWatcher.DisplaySettingsChanged), "MonitorWatcher", null, Action, true, false);
        WriteObject(subscriber);

        if (Duration > TimeSpan.Zero) {
            var timer = new Timer(Duration.TotalMilliseconds) { AutoReset = false };
            timer.Elapsed += (_, _) => {
                Events.UnsubscribeEvent(subscriber);
                watcher.Dispose();
                timer.Dispose();
            };
            timer.Start();
        }
    }
}



#endif
