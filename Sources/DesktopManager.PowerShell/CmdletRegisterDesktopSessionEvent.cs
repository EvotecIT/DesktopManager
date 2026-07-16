namespace DesktopManager.PowerShell;

/// <summary>Registers for meaningful current-session changes.</summary>
[Cmdlet(VerbsLifecycle.Register, "DesktopSessionEvent")]
public sealed class CmdletRegisterDesktopSessionEvent : PSCmdlet {
    /// <summary><para type="description">Optional script block invoked for each change.</para></summary>
    [Parameter]
    public ScriptBlock Action;

    /// <summary><para type="description">Optional duration before automatic unregistration.</para></summary>
    [Parameter]
    public TimeSpan Duration;

    /// <summary><para type="description">Polling interval used to observe session state.</para></summary>
    [Parameter]
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Registers the session watcher.</summary>
    protected override void BeginProcessing() {
        var watcher = new DesktopSessionWatcher(Interval);
        PSEventSubscriber subscriber = Events.SubscribeEvent(watcher, nameof(DesktopSessionWatcher.Changed), "DesktopSession", null, Action, true, false);
        WriteObject(subscriber);
        EventSubscriptionExpiration.Schedule(Duration, () => {
            try {
                Events.UnsubscribeEvent(subscriber);
            } finally {
                watcher.Dispose();
            }
        });
    }
}
