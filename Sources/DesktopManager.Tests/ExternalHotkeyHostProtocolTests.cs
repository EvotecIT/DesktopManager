using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
/// <summary>Tests the out-of-process hotkey host wire protocol.</summary>
public class ExternalHotkeyHostProtocolTests {
    [TestMethod]
    /// <summary>Serializes register commands with stable camel-case JSON fields.</summary>
    public void RegisterCommand_SerializesWithStableFieldNames() {
        var command = new ExternalHotkeyHostCommand {
            Type = ExternalHotkeyHostCommandTypes.Register,
            RegistrationId = 7,
            Modifiers = (int)(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift),
            Key = (int)VirtualKey.VK_5,
            SuppressPotentialChordKeys = true,
            ExclusiveForegroundProcessNames = { "RemoteDesktopManager" }
        };

        string json = JsonSerializer.Serialize(command, ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostCommand);

        StringAssert.Contains(json, "\"type\":\"register\"");
        StringAssert.Contains(json, "\"registrationId\":7");
        StringAssert.Contains(json, "\"suppressPotentialChordKeys\":true");
        StringAssert.Contains(json, "\"exclusiveForegroundProcessNames\":[\"RemoteDesktopManager\"]");
    }

    [TestMethod]
    /// <summary>Deserializes trigger events and preserves the captured foreground handle.</summary>
    public void TriggeredEvent_DeserializesForegroundHandle() {
        const string json = "{\"type\":\"triggered\",\"registrationId\":3,\"foregroundWindowHandle\":4660}";

        ExternalHotkeyHostEvent? hotkeyEvent = JsonSerializer.Deserialize(
            json,
            ExternalHotkeyHostJsonContext.Default.ExternalHotkeyHostEvent);

        Assert.IsNotNull(hotkeyEvent);
        Assert.AreEqual(ExternalHotkeyHostEventTypes.Triggered, hotkeyEvent.Type);
        Assert.AreEqual(3, hotkeyEvent.RegistrationId);
        Assert.AreEqual(4660, hotkeyEvent.ForegroundWindowHandle);
    }
}
