using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    private static WindowSelectionCriteria ReadWorkflowFocusCriteria(JsonElement element) {
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, "windowTitle") ?? "*",
            ProcessNamePattern = ReadOptionalString(element, "processName") ?? "*",
            ClassNamePattern = ReadOptionalString(element, "className") ?? "*",
            ProcessId = ReadInt(element, "processId"),
            Handle = ReadOptionalString(element, "handle"),
            Active = ReadBool(element, "activeWindow"),
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = false,
            All = false
        };
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault) {
        return ReadWindowCriteria(element, includeEmptyDefault, "windowTitle", "processName", "className", "processId", "handle");
    }

    private static WindowSelectionCriteria ReadWindowCriteria(JsonElement element, bool includeEmptyDefault, string titleProperty, string processNameProperty, string classNameProperty, string processIdProperty, string handleProperty) {
        return new WindowSelectionCriteria {
            TitlePattern = ReadOptionalString(element, titleProperty) ?? "*",
            ProcessNamePattern = ReadOptionalString(element, processNameProperty) ?? "*",
            ClassNamePattern = ReadOptionalString(element, classNameProperty) ?? "*",
            ProcessId = ReadInt(element, processIdProperty),
            Handle = ReadOptionalString(element, handleProperty),
            Active = ReadBool(element, "activeWindow"),
            IncludeHidden = ReadBool(element, "includeHidden"),
            IncludeCloaked = !ReadBool(element, "excludeCloaked"),
            IncludeOwned = !ReadBool(element, "excludeOwned"),
            IncludeEmptyTitles = ReadNullableBool(element, "includeEmpty") ?? includeEmptyDefault,
            All = ReadBool(element, "all")
        };
    }

    private static object StopWindowKeepAlive(JsonElement arguments) {
        bool allSessions = ReadBool(arguments, "allSessions");
        if (allSessions) {
            if (HasWindowSelector(arguments) || ReadBool(arguments, "all")) {
                throw new CommandLineException("Cannot combine 'allSessions' with window selectors or 'all'.");
            }

            return DesktopOperations.StopAllWindowKeepAlive();
        }

        return DesktopOperations.StopWindowKeepAlive(ReadWindowCriteria(arguments, true));
    }

    private static bool HasWindowSelector(JsonElement arguments) {
        return !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "windowTitle")) ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "processName")) ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "className")) ||
               ReadInt(arguments, "processId").HasValue ||
               !string.IsNullOrWhiteSpace(ReadOptionalString(arguments, "handle")) ||
               ReadBool(arguments, "activeWindow") ||
               ReadNullableBool(arguments, "includeEmpty").HasValue ||
               ReadBool(arguments, "includeHidden") ||
               ReadBool(arguments, "excludeCloaked") ||
               ReadBool(arguments, "excludeOwned");
    }

    private static ControlSelectionCriteria ReadControlCriteria(JsonElement element) {
        return new ControlSelectionCriteria {
            ClassNamePattern = ReadOptionalString(element, "controlClassName") ?? "*",
            TextPattern = ReadOptionalString(element, "controlText") ?? "*",
            ValuePattern = ReadOptionalString(element, "controlValue") ?? "*",
            Id = ReadInt(element, "controlId"),
            Handle = ReadOptionalString(element, "controlHandle"),
            AutomationIdPattern = ReadOptionalString(element, "controlAutomationId") ?? "*",
            ControlTypePattern = ReadOptionalString(element, "controlType") ?? "*",
            FrameworkIdPattern = ReadOptionalString(element, "controlFrameworkId") ?? "*",
            IsEnabled = ReadNullableBool(element, "isEnabled"),
            IsKeyboardFocusable = ReadNullableBool(element, "isKeyboardFocusable"),
            SupportsBackgroundClick = ReadNullableBool(element, "supportsBackgroundClick"),
            SupportsBackgroundText = ReadNullableBool(element, "supportsBackgroundText"),
            SupportsBackgroundKeys = ReadNullableBool(element, "supportsBackgroundKeys"),
            SupportsForegroundInputFallback = ReadNullableBool(element, "supportsForegroundInputFallback"),
            EnsureForegroundWindow = ReadBool(element, "ensureForegroundWindow"),
            AllowForegroundInputFallback = ReadBool(element, "allowForegroundInput"),
            UiAutomation = ReadBool(element, "uiAutomation"),
            IncludeUiAutomation = ReadBool(element, "includeUiAutomation"),
            All = ReadBool(element, "all")
        };
    }

    private static object CallClickWindowPoint(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? visualBaselineName = ReadOptionalString(arguments, "visualBaselineName");
        if (!string.IsNullOrWhiteSpace(visualBaselineName)) {
            return DesktopOperations.ClickWindowVisualBaseline(
                criteria,
                visualBaselineName,
                ReadOptionalString(arguments, "button") ?? "left",
                ReadBool(arguments, "activate"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadDouble(arguments, "baselineMaxAverageDifference") ?? 12.0,
                ReadInt(arguments, "baselineDifferenceThreshold") ?? 24,
                ReadInt(arguments, "baselineScanStep") ?? 8,
                ReadMutationArtifactOptions(arguments));
        }

        string? ocrText = ReadOptionalString(arguments, "ocrText");
        if (!string.IsNullOrWhiteSpace(ocrText)) {
            return DesktopOperations.ClickWindowText(
                criteria,
                ocrText,
                ReadOptionalString(arguments, "button") ?? "left",
                ReadBool(arguments, "activate"),
                ReadBool(arguments, "ocrContains"),
                ReadOptionalString(arguments, "ocrTargetName"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadOptionalString(arguments, "ocrLanguageTag"),
                ReadMutationArtifactOptions(arguments));
        }

        string? targetName = ReadOptionalString(arguments, "targetName");
        if (!string.IsNullOrWhiteSpace(targetName)) {
            return DesktopOperations.ClickWindowTarget(
                criteria,
                targetName,
                ReadOptionalString(arguments, "button") ?? "left",
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.ClickWindowPoint(
            criteria,
            ReadInt(arguments, "x"),
            ReadInt(arguments, "y"),
            ReadDouble(arguments, "xRatio"),
            ReadDouble(arguments, "yRatio"),
            ReadOptionalString(arguments, "button") ?? "left",
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static object CallDragWindowPoints(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? startVisualBaselineName = ReadOptionalString(arguments, "startVisualBaselineName");
        if (!string.IsNullOrWhiteSpace(startVisualBaselineName)) {
            return DesktopOperations.DragWindowVisualBaselines(
                criteria,
                startVisualBaselineName,
                ReadRequiredString(arguments, "endVisualBaselineName"),
                ReadOptionalString(arguments, "button") ?? "left",
                ReadInt(arguments, "stepDelayMs") ?? 0,
                ReadBool(arguments, "activate"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadDouble(arguments, "baselineMaxAverageDifference") ?? 12.0,
                ReadInt(arguments, "baselineDifferenceThreshold") ?? 24,
                ReadInt(arguments, "baselineScanStep") ?? 8,
                ReadMutationArtifactOptions(arguments));
        }

        string? startOcrText = ReadOptionalString(arguments, "startOcrText");
        if (!string.IsNullOrWhiteSpace(startOcrText)) {
            return DesktopOperations.DragWindowText(
                criteria,
                startOcrText,
                ReadRequiredString(arguments, "endOcrText"),
                ReadOptionalString(arguments, "button") ?? "left",
                ReadInt(arguments, "stepDelayMs") ?? 0,
                ReadBool(arguments, "activate"),
                ReadBool(arguments, "ocrContains"),
                ReadOptionalString(arguments, "startOcrTargetName"),
                ReadOptionalString(arguments, "endOcrTargetName"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadOptionalString(arguments, "ocrLanguageTag"),
                ReadMutationArtifactOptions(arguments));
        }

        string? startTargetName = ReadOptionalString(arguments, "startTargetName");
        if (!string.IsNullOrWhiteSpace(startTargetName)) {
            return DesktopOperations.DragWindowTargets(
                criteria,
                startTargetName,
                ReadRequiredString(arguments, "endTargetName"),
                ReadOptionalString(arguments, "button") ?? "left",
                ReadInt(arguments, "stepDelayMs") ?? 0,
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.DragWindowPoints(
            criteria,
            ReadInt(arguments, "startX"),
            ReadInt(arguments, "startY"),
            ReadDouble(arguments, "startXRatio"),
            ReadDouble(arguments, "startYRatio"),
            ReadInt(arguments, "endX"),
            ReadInt(arguments, "endY"),
            ReadDouble(arguments, "endXRatio"),
            ReadDouble(arguments, "endYRatio"),
            ReadOptionalString(arguments, "button") ?? "left",
            ReadInt(arguments, "stepDelayMs") ?? 0,
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static object CallScrollWindowPoint(JsonElement arguments) {
        WindowSelectionCriteria criteria = ReadWindowCriteria(arguments, true);
        string? visualBaselineName = ReadOptionalString(arguments, "visualBaselineName");
        string? targetName = ReadOptionalString(arguments, "targetName");
        int delta = ReadInt(arguments, "delta") ?? throw new CommandLineException("Property 'delta' is required.");
        if (!string.IsNullOrWhiteSpace(visualBaselineName)) {
            return DesktopOperations.ScrollWindowVisualBaseline(
                criteria,
                visualBaselineName,
                delta,
                ReadBool(arguments, "activate"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadDouble(arguments, "baselineMaxAverageDifference") ?? 12.0,
                ReadInt(arguments, "baselineDifferenceThreshold") ?? 24,
                ReadInt(arguments, "baselineScanStep") ?? 8,
                ReadMutationArtifactOptions(arguments));
        }

        string? ocrText = ReadOptionalString(arguments, "ocrText");
        if (!string.IsNullOrWhiteSpace(ocrText)) {
            return DesktopOperations.ScrollWindowText(
                criteria,
                ocrText,
                delta,
                ReadBool(arguments, "activate"),
                ReadBool(arguments, "ocrContains"),
                ReadOptionalString(arguments, "ocrTargetName"),
                TryReadProperty(arguments, "clientArea", out _) ? ReadBool(arguments, "clientArea") : true,
                ReadOptionalString(arguments, "ocrLanguageTag"),
                ReadMutationArtifactOptions(arguments));
        }

        if (!string.IsNullOrWhiteSpace(targetName)) {
            return DesktopOperations.ScrollWindowTarget(
                criteria,
                targetName,
                delta,
                ReadBool(arguments, "activate"),
                ReadMutationArtifactOptions(arguments));
        }

        return DesktopOperations.ScrollWindowPoint(
            criteria,
            ReadInt(arguments, "x"),
            ReadInt(arguments, "y"),
            ReadDouble(arguments, "xRatio"),
            ReadDouble(arguments, "yRatio"),
            delta,
            ReadBool(arguments, "activate"),
            ReadBool(arguments, "clientArea"),
            ReadMutationArtifactOptions(arguments));
    }

    private static McpToolDefinition CreateTool(string name, string title, string description, object inputSchema, bool readOnly, bool destructive = false, bool idempotent = false, bool openWorld = false) {
        return new McpToolDefinition {
            Name = name,
            Title = title,
            Description = description,
            InputSchema = inputSchema,
            Annotations = new McpToolAnnotations {
                Title = title,
                ReadOnlyHint = readOnly,
                DestructiveHint = destructive,
                IdempotentHint = idempotent,
                OpenWorldHint = openWorld
            }
        };
    }

    private static object CreateWindowSelectorSchema(bool includeAll, bool includeEmpty) {
        var properties = new Dictionary<string, object> {
            ["windowTitle"] = CreateStringSchema("Window title filter."),
            ["processName"] = CreateStringSchema("Process name filter."),
            ["className"] = CreateStringSchema("Window class filter."),
            ["processId"] = CreateIntegerSchema("Process identifier."),
            ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
            ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
            ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
            ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
            ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows.")
        };

        if (includeEmpty) {
            properties["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.");
        }

        if (includeAll) {
            properties["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.");
        }

        return CreateObjectSchema(properties);
    }

    private static object CreateWindowMutationSelectorSchema(bool includeAll, bool includeEmpty) {
        var properties = new Dictionary<string, object> {
            ["windowTitle"] = CreateStringSchema("Window title filter."),
            ["processName"] = CreateStringSchema("Process name filter."),
            ["className"] = CreateStringSchema("Window class filter."),
            ["processId"] = CreateIntegerSchema("Process identifier."),
            ["handle"] = CreateStringSchema("Window handle in decimal or hexadecimal format."),
            ["activeWindow"] = CreateBooleanSchema("Target only the current foreground window."),
            ["includeHidden"] = CreateBooleanSchema("Include hidden windows."),
            ["excludeCloaked"] = CreateBooleanSchema("Exclude DWM-cloaked windows."),
            ["excludeOwned"] = CreateBooleanSchema("Exclude owned windows.")
        };

        if (includeEmpty) {
            properties["includeEmpty"] = CreateBooleanSchema("Include windows with empty titles.");
        }

        if (includeAll) {
            properties["all"] = CreateBooleanSchema("Apply to all matching windows instead of the first match.");
        }

        return CreateObjectSchema(AddMutationArtifactProperties(properties));
    }

    private static Dictionary<string, object> CreateMonitorSelectorProperties() {
        return new Dictionary<string, object> {
            ["connectedOnly"] = CreateBooleanSchema("Return only connected monitors."),
            ["primaryOnly"] = CreateBooleanSchema("Return only the primary monitor."),
            ["index"] = CreateIntegerSchema("Specific monitor index to return."),
            ["deviceId"] = CreateStringSchema("Specific monitor device identifier to return."),
            ["deviceName"] = CreateStringSchema("Specific monitor device name to return.")
        };
    }

    private static Dictionary<string, object> CreateMonitorMutationProperties(Dictionary<string, object> properties) {
        foreach (KeyValuePair<string, object> property in CreateMonitorSelectorProperties()) {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    private static Dictionary<string, object> AddMutationArtifactProperties(Dictionary<string, object> properties) {
        properties["captureBefore"] = CreateBooleanSchema("Capture a best-effort screenshot before the mutation.");
        properties["captureAfter"] = CreateBooleanSchema("Capture a best-effort screenshot after the mutation.");
        properties["artifactDirectory"] = CreateStringSchema("Optional directory for mutation screenshots.");
        properties["verifyAfter"] = CreateBooleanSchema("Re-query the mutated target and report the observed postcondition after the mutation.");
        properties["verificationTolerancePixels"] = CreateIntegerSchema("Optional geometry verification tolerance in pixels. Providing it also enables post-mutation verification.");
        properties["waitForVisualChange"] = CreateBooleanSchema("When supported, wait for a visible pixel change after the mutation instead of relying on a blind delay.");
        properties["visualTargetName"] = CreateStringSchema("Optional saved window target name that narrows visual-change verification to a specific target region.");
        properties["visualClientArea"] = CreateBooleanSchema("Observe the client area for visible change instead of the full window when no visual target is supplied.");
        properties["visualTimeoutMs"] = CreateIntegerSchema("Maximum time in milliseconds to wait for visible change after the mutation.");
        properties["visualIntervalMs"] = CreateIntegerSchema("Polling interval in milliseconds while waiting for visible change.");
        properties["minimumChangedRatio"] = CreateNumberSchema("Minimum sampled pixel ratio that must change before visual verification succeeds.");
        properties["differenceThreshold"] = CreateIntegerSchema("Per-sample average channel difference that counts as a visible pixel change.");
        return properties;
    }

    private static object CreateObjectSchema(Dictionary<string, object>? properties = null, string[]? required = null) {
        return new {
            type = "object",
            properties = properties ?? new Dictionary<string, object>(),
            required = required ?? Array.Empty<string>()
        };
    }

    private static object CreateStringSchema(string description) {
        return new {
            type = "string",
            description
        };
    }

    private static object CreateIntegerSchema(string description) {
        return new {
            type = "integer",
            description
        };
    }

    private static object CreateNumberSchema(string description) {
        return new {
            type = "number",
            description
        };
    }

    private static object CreateBooleanSchema(string description) {
        return new {
            type = "boolean",
            description
        };
    }

    private static object CreateArraySchema(string description, object items) {
        return new {
            type = "array",
            description,
            items
        };
    }

    private static string[] ExtractProcessPatternsFromFilePath(string filePath) {
        string trimmed = filePath.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) {
            return Array.Empty<string>();
        }

        string leaf = trimmed;
        int lastSlash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        if (lastSlash >= 0 && lastSlash < trimmed.Length - 1) {
            leaf = trimmed.Substring(lastSlash + 1);
        }

        if (string.IsNullOrWhiteSpace(leaf)) {
            return Array.Empty<string>();
        }

        string withoutExtension = leaf.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? leaf.Substring(0, leaf.Length - 4)
            : leaf;

        if (string.Equals(leaf, withoutExtension, StringComparison.OrdinalIgnoreCase)) {
            return new[] { leaf };
        }

        return new[] { leaf, withoutExtension };
    }

    private static string ReadRequiredString(JsonElement element, string propertyName) {
        return ReadOptionalString(element, propertyName) ?? throw new CommandLineException($"Property '{propertyName}' is required.");
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static IReadOnlyList<string> ReadStringList(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array) {
            List<string> values = new();
            foreach (JsonElement item in property.EnumerateArray()) {
                if (item.ValueKind == JsonValueKind.Null) {
                    continue;
                }

                values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
            }

            return values;
        }

        string? single = ReadOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(single) ? Array.Empty<string>() : new[] { single };
    }

    private static int? ReadInt(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int numericValue)) {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out int textValue)) {
            return textValue;
        }

        throw new CommandLineException($"Property '{propertyName}' expects an integer value.");
    }

    private static int? ReadPositiveInteger(JsonElement element, string propertyName) {
        int? value = ReadInt(element, propertyName);
        if (value.HasValue && value.Value <= 0) {
            throw new CommandLineException($"Property '{propertyName}' expects a value greater than 0.");
        }

        return value;
    }

    private static double? ReadDouble(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double numericValue)) {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), out double textValue)) {
            return textValue;
        }

        throw new CommandLineException($"Property '{propertyName}' expects a numeric value.");
    }

    private static byte ReadByte(JsonElement element, string propertyName) {
        int value = ReadInt(element, propertyName) ?? throw new CommandLineException($"Property '{propertyName}' is required.");
        if (value < byte.MinValue || value > byte.MaxValue) {
            throw new CommandLineException($"Property '{propertyName}' expects a value from 0 to 255.");
        }

        return (byte)value;
    }

    private static DisplayOrientation? ReadDisplayOrientation(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalDisplayOrientation(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static WindowPlacementKind ReadWindowPlacement(JsonElement element, string propertyName) {
        string value = ReadRequiredString(element, propertyName);
        return value.ToLowerInvariant() switch {
            "restore" => WindowPlacementKind.Restore,
            "maximize" => WindowPlacementKind.Maximize,
            "left-half" or "lefthalf" => WindowPlacementKind.LeftHalf,
            "right-half" or "righthalf" => WindowPlacementKind.RightHalf,
            "exact-rectangle" or "exactrectangle" or "exact" => WindowPlacementKind.ExactRectangle,
            _ => throw new CommandLineException($"Unsupported placement '{value}'.")
        };
    }

    private static WindowMonitorTargetKind ReadWindowMonitorTarget(JsonElement element, string propertyName) {
        string? value = ReadOptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value)) {
            return WindowMonitorTargetKind.Current;
        }

        return value.ToLowerInvariant() switch {
            "current" => WindowMonitorTargetKind.Current,
            "top-left" or "topleft" => WindowMonitorTargetKind.TopLeft,
            "top-right" or "topright" => WindowMonitorTargetKind.TopRight,
            "bottom-left" or "bottomleft" => WindowMonitorTargetKind.BottomLeft,
            "bottom-right" or "bottomright" => WindowMonitorTargetKind.BottomRight,
            _ => throw new CommandLineException($"Unsupported monitor target '{value}'.")
        };
    }

    private static uint ReadColor(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseRequiredColor(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static bool ReadBool(JsonElement element, string propertyName) {
        return ReadNullableBool(element, propertyName) ?? false;
    }

    private static bool ReadRequiredBool(JsonElement element, string propertyName) {
        bool? value = ReadNullableBool(element, propertyName);
        return value ?? throw new CommandLineException($"Property '{propertyName}' is required.");
    }

    private static MutationArtifactOptions? ReadMutationArtifactOptions(JsonElement element) {
        bool captureBefore = ReadBool(element, "captureBefore");
        bool captureAfter = ReadBool(element, "captureAfter");
        string? artifactDirectory = ReadOptionalString(element, "artifactDirectory");
        bool verifyAfter = ReadBool(element, "verifyAfter") || ReadInt(element, "verificationTolerancePixels").HasValue;
        int verificationTolerancePixels = ReadInt(element, "verificationTolerancePixels") ?? 10;
        bool waitForVisualChange = ReadBool(element, "waitForVisualChange");
        string? visualTargetName = ReadOptionalString(element, "visualTargetName");
        bool visualClientArea = ReadBool(element, "visualClientArea");
        int visualTimeoutMilliseconds = ReadInt(element, "visualTimeoutMs") ?? 5000;
        int visualIntervalMilliseconds = ReadInt(element, "visualIntervalMs") ?? 100;
        double visualMinimumChangedRatio = ReadDouble(element, "minimumChangedRatio") ?? 0.01;
        int visualDifferenceThreshold = ReadInt(element, "differenceThreshold") ?? 24;
        if (!captureBefore && !captureAfter && string.IsNullOrWhiteSpace(artifactDirectory) && !verifyAfter && !waitForVisualChange) {
            return null;
        }

        return new MutationArtifactOptions {
            CaptureBefore = captureBefore,
            CaptureAfter = captureAfter,
            ArtifactDirectory = artifactDirectory,
            VerifyAfter = verifyAfter,
            VerificationTolerancePixels = verificationTolerancePixels,
            WaitForVisualChange = waitForVisualChange,
            VisualTargetName = visualTargetName,
            VisualClientArea = visualClientArea,
            VisualTimeoutMilliseconds = visualTimeoutMilliseconds,
            VisualIntervalMilliseconds = visualIntervalMilliseconds,
            VisualMinimumChangedRatio = visualMinimumChangedRatio,
            VisualDifferenceThreshold = visualDifferenceThreshold
        };
    }

    private static bool? ReadNullableBool(JsonElement element, string propertyName) {
        if (!TryReadProperty(element, propertyName, out JsonElement property) || property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True) {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False) {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Property '{propertyName}' expects a boolean value.");
    }

    private static TaskbarPosition? ReadTaskbarPosition(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalTaskbarPosition(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static DesktopWallpaperPosition? ReadWallpaperPosition(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalWallpaperPosition(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static DesktopSlideshowDirection? ReadSlideshowDirection(JsonElement element, string propertyName) {
        return DesktopValueParser.ParseOptionalSlideshowDirection(ReadOptionalString(element, propertyName), $"Property '{propertyName}'");
    }

    private static bool TryReadProperty(JsonElement element, string propertyName, out JsonElement property) {
        property = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out property);
    }
}
