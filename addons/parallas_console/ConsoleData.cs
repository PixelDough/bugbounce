using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Parallas.Console;

public static class ConsoleData
{
    public static readonly Dictionary<string, (ConsoleCommandAttribute Command, MethodInfo MethodInfo)> ConsoleCommands = [];

    public static void FetchData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var allMethods = assembly.GetTypes()
            .SelectMany(t => t.GetRuntimeMethods());

        foreach (var consoleCommandMethod in allMethods)
        {
            if (consoleCommandMethod.GetCustomAttribute(typeof(ConsoleCommandAttribute), false) is not ConsoleCommandAttribute
                consoleCommand) continue;
            if (ConsoleCommands.ContainsKey(consoleCommand.Name))
            {
                GD.PrintErr($"Console command already exists with name \"{consoleCommand.Name}\"!");
                continue;
            }
            ConsoleCommands.Add(consoleCommand.Name, (consoleCommand, consoleCommandMethod));

            // GD.Print($"{consoleCommand.Name} in class {consoleCommandMethod.DeclaringType?.Name ?? "UNKNOWN"}");
        }
    }
}
