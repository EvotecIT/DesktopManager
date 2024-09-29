using Spectre.Console;
using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace DesktopManagerSample;
internal class Helpers {
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

    public static void AddLine(string text, string value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

    public static void AddLine(string text, int value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

    public static void AddLine(string text, uint value) {
        AnsiConsole.Write(new Rule($"[blue]{text}[/]: [yellow]{value}[/]"));
    }

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

    private static void AddPropertiesToTable(Table table, string prefix, object obj) {
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
                    var escapedKey = Markup.Escape(entry.Key.ToString());
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
