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

        // var allWords = commandString.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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
            var parameterType = Nullable.GetUnderlyingType(methodParameter.ParameterType) ??
                                methodParameter.ParameterType;
            var parameterDefaultValue = methodParameter.DefaultValue;

            if (i < parameters.Length)
            {
                var item = parameters[i];
                if (item.Value is null) return;

                if (parameterType == typeof(bool))
                {
                    if (bool.TryParse(item.Value, out var boolVal))
                    {
                        parametersArray.Add(boolVal);
                    }
                    else if (item.Value is "0" or "1")
                    {
                        parametersArray.Add(item.Value == "1");
                    }
                    else
                    {
                        PrintError(
                            $"Invalid value provided for parameter \"{methodParameter.Name}\" (found \"{item.Value}\", expected type {parameterType.Name})");
                        return;
                    }
                }
                else if (parameterType.IsEnum)
                {
                    if (Enum.TryParse(parameterType, item.Value, out var enumVal))
                    {
                        parametersArray.Add(enumVal);
                    }
                    else
                    {
                        PrintError(
                            $"Invalid enum value provided for parameter \"{methodParameter.Name}\" (found \"{item.Value}\", expected type {parameterType.Name})");
                        return;
                    }
                }
                else if (parameterType == typeof(float))
                {
                    if (!float.TryParse(item.Value, out var floatValue))
                    {
                        PrintError(
                            $"Expected float (found \"{item.Value}\")");
                        return;
                    }
                    parametersArray.Add(floatValue);
                }
                else if (parameterType == typeof(Vector3))
                {
                    var floats = CommandableUtils.SplitFloats(item.Value);
                    if (floats.Length != 3)
                    {
                        PrintError(
                            $"Incorrect number of scalars in Vector (expected 3, found {floats.Length})");
                        return;
                    }
                    parametersArray.Add(new Vector3(floats[0], floats[1], floats[2]));
                }
                else if (parameterType == typeof(Vector2))
                {
                    var floats = CommandableUtils.SplitFloats(item.Value);
                    if (floats.Length != 2)
                    {
                        PrintError(
                            $"Incorrect number of scalars in Vector (expected 2, found {floats.Length})");
                        return;
                    }
                    parametersArray.Add(new Vector2(floats[0], floats[1]));
                }
                else if (parameterType == typeof(Vector4))
                {
                    var floats = CommandableUtils.SplitFloats(item.Value);
                    if (floats.Length != 4)
                    {
                        PrintError(
                            $"Incorrect number of scalars in Vector (expected 4, found {floats.Length})");
                        return;
                    }
                    parametersArray.Add(new Vector4(floats[0], floats[1], floats[2], floats[3]));
                }
                else if (parameterType.IsAssignableTo(typeof(Node)))
                {
                    parametersArray.Add(GetNode(item.Value));
                }
                else if (parameterType == typeof(NodePath))
                {
                    parametersArray.Add(new NodePath(item.Value));
                }
                else
                {
                    parametersArray.Add(item.Value);
                }
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
            var childrenOfType = GetTree().Root.FindChildren("*", type.Name, true, false);
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
        GD.PushError(errorMessage);
    }

    public enum ConsoleLogLevel
    {
        Normal,
        Verbose
    }
    [ConsoleCommand(
        "console_log_level",
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
        "debug_draw",
        Description = "Sets the DebugDraw mode on the main Viewport.",
        CommandOutput = "Set DebugDraw on Viewport."
    )]
    public void SetDebugDraw(Viewport.DebugDrawEnum debugDrawMode)
    {
        GetViewport().SetDebugDraw(debugDrawMode);
    }

    public static readonly SuggestionItem.SuggestionData[] AllScenePathsValue = [..CommandableUtils.GetFilePathsByExtension("res://", "tscn", true, ["res:///addons"])];
    public static SuggestionItem.SuggestionData[] AllScenePaths() => [..AllScenePathsValue];
    [ConsoleCommand(
        "change_scene"
    )]
    public void ChangeScene([ConsoleParamInfo(AutocompleteMemberName = nameof(AllScenePaths))] string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }

    [ConsoleCommand("collision_shapes")]
    public void ToggleVisibleCollisionShapes()
    {
        GetTree().SetDebugCollisionsHint(true);
    }
}
