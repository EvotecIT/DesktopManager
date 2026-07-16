namespace DesktopManager.PowerShell;

/// <summary>Registers for supported Windows radio state changes.</summary>
[Cmdlet(VerbsLifecycle.Register, "DesktopRadioEvent")]
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.14393.0")]
public sealed class CmdletRegisterDesktopRadioEvent : PSCmdlet {
    /// <summary><para type="description">Optional script block invoked for each state change.</para></summary>
    [Parameter]
    public ScriptBlock Action;

    /// <summary><para type="description">Optional duration before automatic unregistration.</para></summary>
    [Parameter]
    public TimeSpan Duration;

    /// <summary>Registers the radio watcher.</summary>
    protected override void BeginProcessing() {
        var service = new RadioService();
        service.StartMonitoringAsync().GetAwaiter().GetResult();
        PSEventSubscriber subscriber = Events.SubscribeEvent(service, nameof(RadioService.StateChanged), "DesktopRadio", null, Action, true, false);
        WriteObject(subscriber);
        EventSubscriptionExpiration.Schedule(Duration, () => {
            try {
                Events.UnsubscribeEvent(subscriber);
            } finally {
                service.Dispose();
            }
        });
    }
}
