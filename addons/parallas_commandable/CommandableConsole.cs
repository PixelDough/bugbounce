using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parallas.Commandable;
[GlobalClass]
public partial class CommandableConsole : Control
{
    public static CommandableConsole Instance { get; private set; }

    [Export(PropertyHint.InputName)] private String _inputToggle;
    [Export] private AutoComplete _autoComplete;

    private readonly List<string> _historyStrings = [];
    private Control _consolePanel;
    private RichTextLabel _commandHistory;
    private CommandInput _commandInput;

    private float _offsetX = 0f;
    private Tween _tween;

    public bool IsOpen { get; private set; } = false;

    public bool ShowVerboseLogging { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _consolePanel = GetNode<Control>("%console_panel");
        _commandHistory = GetNode<RichTextLabel>("%command_history");
        _commandInput = GetNode<CommandInput>("%command_input");
        _offsetX = -_consolePanel.Size.X;
        Position = Position with { X = _offsetX };

        _commandInput.TextChanged += TextChanged;
        _commandInput.TextSubmitted += TextSubmitted;

        GetCanvasLayerNode()?.SetLayer(int.MaxValue);

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
        if (_tween is not null && _tween.IsValid() && _tween.IsRunning()) _tween.Kill();
        _tween = CreateTween();
        _tween
            .TweenProperty(this, PropertyName._offsetX.ToString(), 0f, 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        _commandInput.GrabFocus();
        _commandInput.Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        if (_tween is not null && _tween.IsValid() && _tween.IsRunning()) _tween.Kill();
        _tween = CreateTween();
        _tween
            .TweenProperty(this, PropertyName._offsetX.ToString(), -_consolePanel.Size.X, 0.05f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);

        ClearValues();

        _commandInput.ReleaseFocus();
    }

    public void CallCommand(string commandString)
    {
        CommandWord[] allWords =
        [
            ..CommandInput.SplitCommandString(commandString)
                .Where(w => !w.IsNullOrEmpty())
                .Select(w => w.Trim('"'))
        ];

        if (allWords.Length <= 0)
        {
            PrintError("No command provided.");
            return;
        }

        var commandName = allWords[0];
        if (!ConsoleData.ConsoleCommands.TryGetValue(commandName.Value, out var commandMethodPair))
        {
            PrintError($"Invalid command provided: \"{commandName.Value}\"");
            return;
        }
        var command = commandMethodPair.Command;
        var methodInfo = commandMethodPair.MethodInfo;

        var parameters = allWords[1..allWords.Length];
        var methodParameters = methodInfo.GetParameters();
        int requiredCount = methodParameters.Count(p => !p.IsOptional);
        var optionalParameters = methodParameters[requiredCount..];
        if (parameters.Length < requiredCount)
        {
            PrintError($"Not enough parameters provided (found {parameters.Length}, expected {requiredCount}).");
            return;
        }
        if (parameters.Length > methodParameters.Length)
        {
            PrintError($"Too many parameters provided (found {parameters.Length}, expected {methodParameters.Length}).");
            return;
        }

        List<object> parametersArray = [];
        for (var i = 0; i < methodParameters.Length; i++)
        {
            var methodParameter = methodParameters[i];
            var parameterDefaultValue = methodParameter.DefaultValue;

            if (i < parameters.Length)
            {
                var item = parameters[i];
                if (CommandableUtils.TryGetParamValueFromString(item, methodParameter, out var newValue))
                    parametersArray.Add(newValue);
                else
                    return;
            }
            else
            {
                parametersArray.Add(parameterDefaultValue);
            }
        }

        var type = methodInfo.DeclaringType!;
        if (!methodInfo.IsStatic)
        {
            if (methodInfo.DeclaringType!.GetCustomAttribute<GlobalClassAttribute>() is null)
            {
                PrintError($"The [GlobalClass] attribute is required for non-static commands to be run on nodes of type \"{methodInfo.DeclaringType.FullName}\".");
                return;
            }
            var childrenOfType = CommandableUtils.GetAllChildren(GetTree().Root, methodInfo.DeclaringType);
            PrintTextVerbose($"Found {childrenOfType.Count} node of type {type.Name}");
            foreach (var node in childrenOfType)
            {
                PrintTextVerbose($"Calling method {methodInfo.Name} on node {node.Name}");
                try
                {
                    methodInfo.Invoke(node, [..parametersArray]);
                }
                catch (Exception e)
                {
                    PrintError($"Error invoking function: {e}");
                    GD.PushError(e);
                }
            }
        }
        else
        {
            methodInfo.Invoke(null, [..parametersArray]);
        }
        PrintTextVerbose("Command successfully executed.");
        if (command.CommandOutput is not null)
            PrintText(command.CommandOutput);
    }

    public static void PrintText(string text)
    {
        Instance._historyStrings.Add($"[color=white]{text}");
        Instance._commandHistory.Text = String.Join('\n', Instance._historyStrings);
        GD.Print(text);
    }

    public static void PrintTextVerbose(string text)
    {
        if (!Instance.ShowVerboseLogging) return;
        PrintText($"[color=gray]{text}");
    }

    public static void PrintError(string errorMessage)
    {
        PrintText($"[color=red]Error: {errorMessage}");
        GD.PushError(errorMessage);
    }

    public enum ConsoleLogLevel
    {
        Normal,
        Verbose
    }
    [ConsoleCommand(
        "console.set_log_level",
        Description = "Sets the logging level of the dev console's own output."
    )]
    public void SetConsoleLogLevel(ConsoleLogLevel consoleLogLevel)
    {
        ShowVerboseLogging = consoleLogLevel == ConsoleLogLevel.Verbose;
    }

    private void TextChanged(string text)
    {
        _autoComplete.Refresh();
        _autoComplete.SetIsOpen(_commandInput.Text.Length > 0);
    }

    private void TextSubmitted(string text)
    {
        PrintText("");
        PrintText($"[color=cyan]>{text}");
        CallCommand(text);
        ClearValues();
    }

    private void ClearValues()
    {
        _commandInput.ClearValues();
        _commandInput.Clear();
        _autoComplete.Close();
    }

    [ConsoleCommand(
        "godot.set_debug_draw",
        Description = "Sets the DebugDraw mode on the main Viewport.",
        CommandOutput = "Set DebugDraw on Viewport."
    )]
    public void SetDebugDraw(Viewport.DebugDrawEnum debugDrawMode)
    {
        GetViewport().SetDebugDraw(debugDrawMode);
    }

    [ConsoleCommand(
        "godot.change_scene",
        Description = "Changes the running scene to a new instance of the given PackedScene."
    )]
    public void ChangeScene([FileFilter(AllowedExtensions = ["tscn"], IgnoreDirectories = ["res:///addons"])] PackedScene scene)
    {
        GetTree().ChangeSceneToPacked(scene);
    }

    [ConsoleCommand(
        "godot.set_debug_collisions",
        Description = "Sets the visibility of collision shapes."
    )]
    public void ToggleVisibleCollisionShapes(bool? state = null)
    {
        GetTree().SetDebugCollisionsHint(state ?? !GetTree().DebugCollisionsHint);
    }
}
