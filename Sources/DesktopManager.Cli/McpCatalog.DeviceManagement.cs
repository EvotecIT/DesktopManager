using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static McpToolDefinition[] CreateDeviceManagementTools() {
        Dictionary<string, object> queryProperties = CreateDeviceQueryProperties();
        return new[] {
            CreateTool(
                "list_devices",
                "List Plug and Play Devices",
                "List local Plug and Play device instances using exact optional filters.",
                CreateObjectSchema(queryProperties),
                readOnly: true),
            CreateTool(
                "get_device",
                "Get Plug and Play Device",
                "Get one exact device with relations, stack, resources, interfaces, and unified properties.",
                CreateObjectSchema(new Dictionary<string, object> {
                    ["instanceId"] = CreateStringSchema("Exact Plug and Play device instance identifier; wildcards are rejected.")
                }, new[] { "instanceId" }),
                readOnly: true),
            CreateTool(
                "list_device_drivers",
                "List Compatible Device Drivers",
                "List ranked driver candidates Windows considers compatible with one exact device instance.",
                CreateObjectSchema(new Dictionary<string, object> {
                    ["instanceId"] = CreateStringSchema("Exact Plug and Play device instance identifier; wildcards are rejected.")
                }, new[] { "instanceId" }),
                readOnly: true),
            CreateTool(
                "list_driver_packages",
                "List Driver Store Packages",
                "List third-party Windows Driver Store packages using exact optional filters.",
                CreateObjectSchema(new Dictionary<string, object> {
                    ["publishedInfName"] = CreateStringSchema("Exact published INF name such as oem42.inf."),
                    ["classGuid"] = CreateStringSchema("Optional exact setup class GUID."),
                    ["includeFiles"] = CreateBooleanSchema("Include package files."),
                    ["includeDevices"] = CreateBooleanSchema("Include devices currently using each package.")
                }),
                readOnly: true),
            CreateTool(
                "list_device_classes",
                "List Device Setup Classes",
                "List Windows device setup classes and upper/lower filter chains.",
                CreateObjectSchema(),
                readOnly: true),
            CreateTool(
                "list_device_containers",
                "List Device Containers",
                "Group matching Plug and Play instances by Windows device container.",
                CreateObjectSchema(queryProperties),
                readOnly: true)
        };
    }

    private static bool TryCallDeviceManagementTool(string name, JsonElement arguments, out object result) {
        var service = new DeviceManagementService();
        try {
            switch (name) {
                case "list_devices":
                    result = service.GetDevices(ReadDeviceQuery(arguments));
                    return true;
                case "get_device":
                    result = service.GetDevice(ReadRequiredString(arguments, "instanceId"));
                    return true;
                case "list_device_drivers":
                    result = service.GetCompatibleDrivers(ReadRequiredString(arguments, "instanceId"));
                    return true;
                case "list_driver_packages":
                    result = service.GetDriverPackages(new DesktopDriverPackageQuery {
                        PublishedInfName = ReadOptionalString(arguments, "publishedInfName"),
                        ClassGuid = ReadOptionalGuid(arguments, "classGuid"),
                        IncludeFiles = ReadBool(arguments, "includeFiles"),
                        IncludeDevices = ReadBool(arguments, "includeDevices")
                    });
                    return true;
                case "list_device_classes":
                    result = service.GetDeviceClasses();
                    return true;
                case "list_device_containers":
                    result = service.GetDeviceContainers(ReadDeviceQuery(arguments));
                    return true;
                default:
                    result = null!;
                    return false;
            }
        } catch (ArgumentException ex) {
            throw new CommandLineException(ex.Message);
        } catch (InvalidOperationException ex) {
            throw new CommandLineException(ex.Message);
        }
    }

    private static DesktopDeviceQuery ReadDeviceQuery(JsonElement arguments) {
        int? problemCode = ReadInt(arguments, "problemCode");
        if (problemCode < 0) {
            throw new CommandLineException("Property 'problemCode' expects a non-negative integer value.");
        }
        return new DesktopDeviceQuery {
            InstanceId = ReadOptionalString(arguments, "instanceId"),
            DeviceId = ReadOptionalString(arguments, "deviceId"),
            ClassName = ReadOptionalString(arguments, "className"),
            ClassGuid = ReadOptionalGuid(arguments, "classGuid"),
            EnumeratorName = ReadOptionalString(arguments, "enumeratorName"),
            Present = ReadNullableBool(arguments, "present"),
            HasProblem = ReadNullableBool(arguments, "hasProblem"),
            ProblemCode = problemCode.HasValue ? checked((uint)problemCode.Value) : null,
            IncludeRelations = ReadBool(arguments, "includeRelations"),
            IncludeStack = ReadBool(arguments, "includeStack"),
            IncludeResources = ReadBool(arguments, "includeResources"),
            IncludeInterfaces = ReadBool(arguments, "includeInterfaces"),
            IncludeProperties = ReadBool(arguments, "includeProperties")
        };
    }

    private static Guid? ReadOptionalGuid(JsonElement arguments, string propertyName) {
        string? value = ReadOptionalString(arguments, propertyName);
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }
        if (Guid.TryParse(value, out Guid parsed)) {
            return parsed;
        }
        throw new CommandLineException($"Property '{propertyName}' expects a GUID value.");
    }

    private static Dictionary<string, object> CreateDeviceQueryProperties() {
        return new Dictionary<string, object> {
            ["instanceId"] = CreateStringSchema("Exact Plug and Play device instance identifier; wildcards are rejected."),
            ["deviceId"] = CreateStringSchema("Exact hardware or compatible identifier."),
            ["className"] = CreateStringSchema("Exact setup class name."),
            ["classGuid"] = CreateStringSchema("Exact setup class GUID."),
            ["enumeratorName"] = CreateStringSchema("Exact bus enumerator name."),
            ["present"] = CreateBooleanSchema("Filter present or non-present devices."),
            ["hasProblem"] = CreateBooleanSchema("Filter devices by problem state."),
            ["problemCode"] = CreateIntegerSchema("Exact Configuration Manager problem code."),
            ["includeRelations"] = CreateBooleanSchema("Include parent, child, sibling, and dependency relations."),
            ["includeStack"] = CreateBooleanSchema("Include the effective driver stack."),
            ["includeResources"] = CreateBooleanSchema("Include allocated hardware resources."),
            ["includeInterfaces"] = CreateBooleanSchema("Include registered device interfaces."),
            ["includeProperties"] = CreateBooleanSchema("Include every available unified device property.")
        };
    }
}
