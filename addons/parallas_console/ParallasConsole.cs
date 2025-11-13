using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Array = Godot.Collections.Array;

namespace Parallas.Console;
[GlobalClass]
public partial class ParallasConsole : Control
{
    public static ParallasConsole Instance { get; private set; }

    public bool ShowVerboseLogging { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        ConsoleData.FetchData();
    }

    public void CallCommand(string commandString)
    {
        var allWords = commandString.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (allWords.Length <= 0)
        {
            PrintText("Error: No command provided.");
            return;
        }

        var parameters = allWords[1..allWords.Length];
        var parametersArray = new Array();
        parametersArray.AddRange(parameters);

        var commandName = allWords[0];
        if (!ConsoleData.ConsoleCommands.TryGetValue(commandName, out var commandMethodPair))
        {
            PrintText("Error: Invalid command provided.");
            return;
        }
        var command = commandMethodPair.Command;
        var methodInfo = commandMethodPair.MethodInfo;

        if (!methodInfo.IsStatic)
        {
            var type = methodInfo.DeclaringType!;
            var childrenOfType = GetTree().Root.FindChildren("*", type.Name, true, false);
            PrintTextVerbose($"Found {childrenOfType.Count} node of type {type.Name}");
            foreach (var node in childrenOfType)
            {
                PrintTextVerbose($"Calling method {methodInfo.Name} on node {node.Name}");
                node.Callv(methodInfo.Name, parametersArray);
            }
        }
        PrintTextVerbose("Command successfully executed.");
    }

    public void PrintText(string text)
    {
        GD.Print(text);
    }

    public void PrintTextVerbose(string text)
    {
        if (!ShowVerboseLogging) return;
        PrintText(text);
    }

    [ConsoleCommand("dev_log_verbose", "Sets whether to log console process outputs.")]
    public void SetVerboseLogging(bool value)
    {
        ShowVerboseLogging = value;
        GD.Print($"verbose logging set to {value}.");
    }
}
