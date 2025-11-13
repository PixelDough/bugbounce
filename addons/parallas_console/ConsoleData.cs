using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parallas.Console;

public static class ConsoleData
{
    public static MethodInfo[] ConsoleCommandMethods = [];

    public static readonly Dictionary<string, (ConsoleCommand Command, MethodInfo MethodInfo)> ConsoleCommands = [];

    public static void FetchData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var allMethods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods());

        ConsoleCommandMethods = allMethods
            .Where(m => m.GetCustomAttributes(typeof(ConsoleCommand), false).Length > 0)
            .ToArray();

        foreach (var consoleCommandMethod in ConsoleCommandMethods)
        {
            if (consoleCommandMethod.GetCustomAttribute(typeof(ConsoleCommand), false) is not ConsoleCommand
                consoleCommand) continue;
            ConsoleCommands.Add(consoleCommand.Name, (consoleCommand, consoleCommandMethod));

            // GD.Print($"{consoleCommand.Name} in class {consoleCommandMethod.DeclaringType?.Name ?? "UNKNOWN"}");
        }
    }
}
