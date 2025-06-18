using System.Collections;
using Spectre.Console;

namespace DesktopManager.Example;
/// <summary>
/// Provides helper methods for displaying properties and adding lines to the console.
/// </summary>
internal class Helpers {
    /// <summary>
    /// Displays the properties of an object in the console.
    /// </summary>
    /// <param name="analysisOf">The description of the object being analyzed.</param>
    /// <param name="obj">The object whose properties are to be displayed.</param>
    public static void ShowProperties(string analysisOf, object obj) {
        Console.WriteLine("----");
        Console.WriteLine($"Analysis of {analysisOf}:");
        var properties = obj.GetType().GetProperties();
        foreach (var property in properties) {
            var value = property.GetValue(obj);
            if (value is IList listValue) {
                Console.WriteLine($"- {property.Name}:");
                foreach (var item in listValue) {
                    Console.WriteLine($"  * {item}");
                }
            } else {
                Console.WriteLine($"- {property.Name}: {value}");
            }
        }
    }

    /// <summary>
    /// Adds a line to the console with a specified text and string value.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="value">The string value to display.</param>
    public static void AddLine(string text, string value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

    /// <summary>
    /// Adds a line to the console with a specified text and integer value.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="value">The integer value to display.</param>
    public static void AddLine(string text, int value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

    /// <summary>
    /// Adds a line to the console with a specified text and unsigned integer value.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="value">The unsigned integer value to display.</param>
    public static void AddLine(string text, uint value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

    /// <summary>
    /// Displays the properties of an object or a collection of objects in a table format.
    /// </summary>
    /// <param name="analysisOf">The description of the object being analyzed.</param>
    /// <param name="objs">The object or collection of objects whose properties are to be displayed.</param>
    /// <param name="perProperty">Indicates whether to display properties per property.</param>
    public static void ShowPropertiesTable(string analysisOf, object objs, bool perProperty = false) {
        var table = new Table();
        table.Border(TableBorder.Rounded);

        table.AddColumn("Property");
        table.AddColumn("Value");

        if (objs is IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) {
                var obj = entry.Value;
                AddPropertiesToTable(table, $"{entry.Key}", obj);
            }
        } else if (objs is IList list) {
            foreach (var obj in list) {
                AddPropertiesToTable(table, obj.GetType().Name, obj);
            }
        } else {
            AddPropertiesToTable(table, objs.GetType().Name, objs);
        }

        var panel = new Panel(table)
            .Header($"Analysis of {analysisOf}")
            .Expand();

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Adds the properties of an object to a table.
    /// </summary>
    /// <param name="table">The table to which properties are added.</param>
    /// <param name="prefix">The prefix for the property names.</param>
    /// <param name="obj">The object whose properties are to be added.</param>
    private static void AddPropertiesToTable(Table table, string prefix, object? obj) {
        if (obj == null) {
            return;
        }

        var properties = obj.GetType().GetProperties();
        foreach (var property in properties) {
            var value = property.GetValue(obj);
            if (value is IList listValue) {
                var listString = string.Join(", ", listValue.Cast<object>());
                table.AddRow(Markup.Escape($"{prefix}.{property.Name}"), Markup.Escape(listString));
            } else if (value is IDictionary dictionaryValue) {
                var nestedTable = new Table().Border(TableBorder.Rounded);
                nestedTable.AddColumn("Key");
                nestedTable.AddColumn("Value");

                foreach (DictionaryEntry entry in dictionaryValue) {
                    var escapedKey = Markup.Escape(entry.Key?.ToString() ?? string.Empty);
                    var escapedValue = Markup.Escape(entry.Value?.ToString() ?? "null");
                    nestedTable.AddRow(escapedKey, escapedValue);
                }
                table.AddRow(new Markup($"{prefix}.{property.Name}"), nestedTable);
            } else if (value != null && value.GetType().IsValueType && !value.GetType().IsPrimitive && !value.GetType().IsEnum) {
                // Handle nested structs
                var nestedTable = new Table().Border(TableBorder.Rounded);
                nestedTable.AddColumn("Property");
                nestedTable.AddColumn("Value");

                AddPropertiesToTable(nestedTable, $"{prefix}.{property.Name}", value);
                table.AddRow(new Markup($"{prefix}.{property.Name}"), nestedTable);
            } else {
                table.AddRow(Markup.Escape($"{prefix}.{property.Name}"), Markup.Escape(value?.ToString() ?? "null"));
            }
        }
    }
}
