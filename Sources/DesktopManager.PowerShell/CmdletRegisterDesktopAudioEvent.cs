using System.Timers;

namespace DesktopManager.PowerShell;

/// <summary>Registers for Core Audio endpoint changes.</summary>
/// <para type="description">Observes endpoint arrival, removal, state, default, and property changes.</para>
[Cmdlet(VerbsLifecycle.Register, "DesktopAudioEvent")]
public sealed class CmdletRegisterDesktopAudioEvent : PSCmdlet {
    /// <summary><para type="description">Optional script block invoked for each notification.</para></summary>
    [Parameter]
    public ScriptBlock Action;

    /// <summary><para type="description">Optional duration before automatic unregistration.</para></summary>
    [Parameter]
    public TimeSpan Duration;

    /// <summary>Registers the endpoint watcher.</summary>
    protected override void BeginProcessing() {
        var watcher = new AudioEndpointWatcher();
        PSEventSubscriber subscriber = Events.SubscribeEvent(watcher, nameof(AudioEndpointWatcher.Changed), "DesktopAudio", null, Action, true, false);
        WriteObject(subscriber);
        DisposeAfterDuration(watcher, subscriber);
    }

    private void DisposeAfterDuration(AudioEndpointWatcher watcher, PSEventSubscriber subscriber) {
        if (Duration <= TimeSpan.Zero) {
            return;
        }
        var timer = new Timer(Duration.TotalMilliseconds) { AutoReset = false };
        timer.Elapsed += (_, _) => {
            Events.UnsubscribeEvent(subscriber);
            watcher.Dispose();
            timer.Dispose();
        };
        timer.Start();
    }
}
