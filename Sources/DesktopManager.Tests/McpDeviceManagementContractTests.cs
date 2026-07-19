#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
using System.Text.Json;

namespace DesktopManager.Tests;

[TestClass]
[SupportedOSPlatform("windows6.0.6000.0")]
public sealed class McpDeviceManagementContractTests {
    private static readonly string[] DeviceToolNames = {
        "list_devices",
        "get_device",
        "list_device_drivers",
        "list_driver_packages",
        "list_device_classes",
        "list_device_containers"
    };

    [TestMethod]
    public void DeviceManagementToolsAreAdvertisedAsReadOnly() {
        Dictionary<string, JsonElement> tools = DesktopManager.Cli.McpCatalog.GetTools()
            .Select(tool => JsonSerializer.SerializeToElement(tool))
            .ToDictionary(tool => tool.GetProperty("name").GetString()!, StringComparer.Ordinal);

        foreach (string name in DeviceToolNames) {
            Assert.IsTrue(tools.ContainsKey(name), $"MCP tool '{name}' was not advertised.");
            Assert.IsTrue(tools[name].GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
            Assert.IsFalse(DesktopManager.Cli.McpCatalog.IsMutatingTool(name));
        }
    }

    [TestMethod]
    public void ListDevicesRejectsWildcardInstanceIdBeforeNativeEnumeration() {
        using JsonDocument document = JsonDocument.Parse("""{"instanceId":"PCI\\*"}""");

        bool succeeded = DesktopManager.Cli.McpCatalog.TryCallTool(
            "list_devices",
            document.RootElement,
            out object result,
            out string? error);

        Assert.IsFalse(succeeded);
        StringAssert.Contains(error, "cannot contain wildcards");
        StringAssert.Contains(JsonSerializer.Serialize(result), "cannot contain wildcards");
    }

    [TestMethod]
    public void DriverPackageToolRejectsInvalidClassGuid() {
        using JsonDocument document = JsonDocument.Parse("""{"classGuid":"not-a-guid"}""");

        bool succeeded = DesktopManager.Cli.McpCatalog.TryCallTool(
            "list_driver_packages",
            document.RootElement,
            out _,
            out string? error);

        Assert.IsFalse(succeeded);
        Assert.AreEqual("Property 'classGuid' expects a GUID value.", error);
    }
}
#endif
