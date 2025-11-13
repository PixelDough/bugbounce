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

    [Export(PropertyHint.InputName)] private String _inputToggle;

    private readonly List<string> _historyStrings = [];
    private Control _consolePanel;
    private RichTextLabel _commandHistory;
    private LineEdit _commandInput;
    private float _offsetX = 0f;

    public bool IsOpen { get; private set; } = false;

    public bool ShowVerboseLogging { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _consolePanel = GetNode<Control>("%console_panel");
        _commandHistory = GetNode<RichTextLabel>("%command_history");
        _commandInput = GetNode<LineEdit>("%command_input");
        _offsetX = -_consolePanel.Size.X;
        Position = Position with { X = _offsetX };

        _commandInput.TextSubmitted += TextSubmitted;

        ConsoleData.FetchData();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Position = Position with { X = _offsetX };

        if (Input.IsActionJustPressed(_inputToggle))
        {
            Toggle();
        }

        // if (Input.MouseMode == Input.MouseModeEnum.Captured)
        // {
        //     MouseFilter = MouseFilterEnum.Ignore;
        //     FocusMode = FocusModeEnum.None;
        //     _commandInput.ReleaseFocus();
        // }
        // else
        // {
        //     MouseFilter = MouseFilterEnum.Stop;
        //     FocusMode = FocusModeEnum.All;
        // }
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        IsOpen = true;
        CreateTween()
            .TweenProperty(this, PropertyName._offsetX.ToString(), 0f, 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        _commandInput.GrabFocus();
    }

    public void Close()
    {
        IsOpen = false;
        CreateTween()
            .TweenProperty(this, PropertyName._offsetX.ToString(), -_consolePanel.Size.X, 0.05f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);

        _commandInput.ReleaseFocus();
    }

    public void CallCommand(string commandString)
    {
        var allWords = commandString.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (allWords.Length <= 0)
        {
            PrintError("No command provided.");
            return;
        }

        var parameters = allWords[1..allWords.Length];
        var parametersArray = new Array();
        parametersArray.AddRange(parameters);

        var commandName = allWords[0];
        if (!ConsoleData.ConsoleCommands.TryGetValue(commandName, out var commandMethodPair))
        {
            PrintError("Invalid command provided.");
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
        if (command.CommandOutput is not null)
            PrintText(command.CommandOutput);
    }

    public string[] GetAutocompleteValues(string autocompleteMethodName, MethodInfo forMethod)
    {
        if (string.IsNullOrEmpty(autocompleteMethodName)) return [];

        var declaringType = forMethod.DeclaringType!;
        if (declaringType.GetMethod(autocompleteMethodName) is not { } autocompleteMethod)
        {
            PrintError($"Autocomplete method \"{autocompleteMethodName}\" not found.");
            return [];
        }
        if (!autocompleteMethod.IsStatic)
        {
            PrintError($"Autocomplete method \"{autocompleteMethodName}\" is not static.");
            return [];
        }

        var result = autocompleteMethod.Invoke(null, null);
        if (result is not string[] resultStrings)
        {
            PrintError($"Autocomplete method \"{autocompleteMethodName}\" did not return an array of strings.");
            return [];
        }

        return resultStrings;
    }

    public void PrintText(string text)
    {
        _historyStrings.Add($"[color=white]{text}");
        _commandHistory.Text = String.Join('\n', _historyStrings);
        GD.Print(text);
    }

    public void PrintTextVerbose(string text)
    {
        if (!ShowVerboseLogging) return;
        PrintText($"[color=gray]{text}");
    }

    public void PrintError(string errorMessage)
    {
        PrintText($"[color=red]Error: {errorMessage}");
    }

    [ConsoleCommand("dev_log_verbose", Description = "Sets whether to log console process outputs.")]
    public void SetVerboseLogging(bool value)
    {
        ShowVerboseLogging = value;
        GD.Print($"verbose logging set to {value}.");
    }

    private void TextSubmitted(string text)
    {
        PrintText("");
        _commandInput.Clear();
        PrintText($"[color=cyan]>{text}");
        CallCommand(text);
    }
}
