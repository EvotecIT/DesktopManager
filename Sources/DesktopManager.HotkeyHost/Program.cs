using System.Text.Json;
using DesktopManager;

namespace DesktopManager.HotkeyHost;

internal static class Program {
    private static readonly object OutputSync = new();
    private static readonly Dictionary<int, int> Registrations = new();

    public static int Main() {
        try {
            _ = LowLevelKeyboardHotkeyService.Instance;
            WriteEvent(new ExternalHotkeyHostEvent { Type = ExternalHotkeyHostEventTypes.Ready });
            RunCommandLoop();
            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void RunCommandLoop() {
        string? line;
        while ((line = Console.ReadLine()) != null) {
            ExternalHotkeyHostCommand? command = JsonSerializer.Deserialize(
                line,
                ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostCommand);
            if (command == null) {
                continue;
            }

            if (string.Equals(command.Type, ExternalHotkeyHostCommandTypes.Shutdown, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            if (string.Equals(command.Type, ExternalHotkeyHostCommandTypes.Register, StringComparison.OrdinalIgnoreCase)) {
                Register(command);
                continue;
            }

            if (string.Equals(command.Type, ExternalHotkeyHostCommandTypes.Unregister, StringComparison.OrdinalIgnoreCase)) {
                Unregister(command.RegistrationId);
            }
        }
    }

    private static void Register(ExternalHotkeyHostCommand command) {
        try {
            var options = new LowLevelKeyboardHotkeyOptions {
                SuppressPotentialChordKeys = command.SuppressPotentialChordKeys,
                ExclusiveForegroundProcessNames = command.ExclusiveForegroundProcessNames
            };
            int serviceId = LowLevelKeyboardHotkeyService.Instance.RegisterHotkey(
                (HotkeyModifiers)command.Modifiers,
                (VirtualKey)command.Key,
                options,
                foregroundHandle => WriteEvent(new ExternalHotkeyHostEvent {
                    Type = ExternalHotkeyHostEventTypes.Triggered,
                    RegistrationId = command.RegistrationId,
                    ForegroundWindowHandle = foregroundHandle.ToInt64()
                }));

            Registrations[command.RegistrationId] = serviceId;
            WriteEvent(new ExternalHotkeyHostEvent {
                Type = ExternalHotkeyHostEventTypes.Registered,
                RegistrationId = command.RegistrationId
            });
        } catch (Exception ex) {
            WriteEvent(new ExternalHotkeyHostEvent {
                Type = ExternalHotkeyHostEventTypes.Error,
                RegistrationId = command.RegistrationId,
                Message = ex.Message
            });
        }
    }

    private static void Unregister(int registrationId) {
        if (!Registrations.TryGetValue(registrationId, out int serviceId)) {
            return;
        }

        LowLevelKeyboardHotkeyService.Instance.UnregisterHotkey(serviceId);
        Registrations.Remove(registrationId);
        WriteEvent(new ExternalHotkeyHostEvent {
            Type = ExternalHotkeyHostEventTypes.Unregistered,
            RegistrationId = registrationId
        });
    }

    private static void WriteEvent(ExternalHotkeyHostEvent hotkeyEvent) {
        lock (OutputSync) {
            Console.WriteLine(JsonSerializer.Serialize(hotkeyEvent, ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostEvent));
            Console.Out.Flush();
        }
    }
}
