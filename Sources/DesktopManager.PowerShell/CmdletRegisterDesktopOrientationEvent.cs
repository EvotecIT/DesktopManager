#if !NETSTANDARD2_0 && !NETSTANDARD2_1
using System;
using System.Management.Automation;
using System.Timers;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Registers for desktop orientation change events.</summary>
/// <para type="synopsis">Registers for desktop orientation change events.</para>
/// <para type="description">Subscribes to orientation changes and returns the event subscription.</para>
/// <example>
///   <summary>Monitor orientation changes for five minutes</summary>
///   <code>Register-DesktopOrientationEvent -Duration (New-TimeSpan -Minutes 5)</code>
/// </example>
[Cmdlet(VerbsLifecycle.Register, "DesktopOrientationEvent")]
[SupportedOSPlatform("windows")]
public sealed class CmdletRegisterDesktopOrientationEvent : PSCmdlet {
    /// <summary>
    /// <para type="description">The script block to run when the event is raised.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public ScriptBlock Action { get; set; }

    /// <summary>
    /// <para type="description">The duration to monitor before automatically unregistering.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public TimeSpan Duration { get; set; }

    /// <summary>Begin processing.</summary>
    protected override void BeginProcessing() {
        var watcher = new MonitorWatcher();
        var subscriber = Events.SubscribeEvent(watcher, nameof(MonitorWatcher.OrientationChanged), "MonitorWatcher", null, Action, true, false);
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
