using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopManager.Cli;

internal static class JsonUtilities {
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string Serialize(object value) {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public static JsonSerializerOptions SerializerOptions => JsonOptions;

    private static JsonSerializerOptions CreateOptions() {
        var options = new JsonSerializerOptions {
            WriteIndented = true
        };
        options.Converters.Add(new IntPtrJsonConverter());
        return options;
    }

    private sealed class IntPtrJsonConverter : JsonConverter<IntPtr> {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string? value = reader.GetString();
            return DesktopHandleParser.Parse(value ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options) {
            writer.WriteStringValue($"0x{value.ToInt64():X}");
        }
    }
}
