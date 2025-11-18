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
    [Export(PropertyHint.InputName)] private String _inputAutoCompleteConfirm;
    [Export(PropertyHint.InputName)] private String _inputAutoCompleteNext;
    [Export(PropertyHint.InputName)] private String _inputAutoCompletePrev;

    private readonly List<string> _historyStrings = [];
    private Control _consolePanel;
    private RichTextLabel _commandHistory;
    private CommandInput _commandInput;
    private Control _autocompleteControl;
    private ScrollContainer _autocompleteScroll;
    private VBoxContainer _autocompleteVbox;
    private SuggestionItem _autocompleteTooltip;

    private float _offsetX = 0f;
    private int _wordIndex = int.MinValue;
    private CommandWord[] _words = [new CommandWord()];
    private string[] _autoCompleteWords = [];
    private readonly List<SuggestionItem> _autoCompleteSuggestionItems = [];
    private int _autoCompleteIndex = 0;
    private bool _showAutoComplete = false;
    private Tween _tween;

    private PackedScene _autocompleteSuggestionScene =
        ResourceLoader.Load<PackedScene>("res://addons/parallas_commandable/suggestion_item.tscn");

    public bool IsOpen { get; private set; } = false;

    public bool ShowVerboseLogging { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _consolePanel = GetNode<Control>("%console_panel");
        _commandHistory = GetNode<RichTextLabel>("%command_history");
        _commandInput = GetNode<CommandInput>("%command_input");
        _autocompleteControl = GetNode<Control>("%autocomplete_control");
        _autocompleteScroll = GetNode<ScrollContainer>("%autocomplete_scroll");
        _autocompleteVbox = GetNode<VBoxContainer>("%autocomplete_vbox");
        _autocompleteTooltip = GetNode<SuggestionItem>("%autocomplete_tooltip");
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

        if (!IsOpen) return;

        if (Input.IsActionJustPressed(_inputAutoCompleteConfirm))
        {
            if (!_showAutoComplete)
            {
                RefreshAutoComplete();
                _showAutoComplete = true;
            }
            else
            {
                var lastWord = _words[_wordIndex];
                // if (char.IsWhiteSpace(_commandInput.Text.LastOrDefault(' '))) lastWordLength = 0;
                var cleanedText = _commandInput.Text.Remove(lastWord.StartIndex, lastWord.Length);
                _commandInput.SetText(cleanedText);
                _commandInput.CaretColumn = lastWord.StartIndex;
                var newString = _autoCompleteWords[_autoCompleteIndex];
                if (newString.Contains(' '))
                {
                    newString = $@"""{newString}""";
                }
                _commandInput.InsertTextAtCaret(newString);
                ClearAutoComplete();
                TextChanged(_commandInput.Text);
            }
        }

        _autocompleteControl.Modulate = _autocompleteControl.Modulate with
        {
            A = ExpDecay(_autocompleteControl.Modulate.A,
                _showAutoComplete ? 1f : 0f, 50f, (float)delta)
        };
        _autocompleteControl.Scale = _autocompleteControl.Scale with
        {
            Y = ExpDecay(
                _autocompleteControl.Scale.Y,
                _showAutoComplete ? 1f : 0f,
                40f,
                (float)delta
            )
        };
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!IsOpen) return;

        if (_autoCompleteSuggestionItems.Count > 0)
        {
            if (@event.IsActionPressed(_inputAutoCompleteNext, true))
                _autoCompleteIndex--;
            if (@event.IsActionPressed(_inputAutoCompletePrev, true))
                _autoCompleteIndex++;

            if (@event.IsEcho())
            {
                _autoCompleteIndex = Mathf.Clamp(_autoCompleteIndex, 0, _autoCompleteSuggestionItems.Count - 1);
            }
            else
            {
                if (_autoCompleteIndex >= _autoCompleteSuggestionItems.Count) _autoCompleteIndex = 0;
                if (_autoCompleteIndex < 0) _autoCompleteIndex = _autoCompleteSuggestionItems.Count - 1;
            }

            // _autocompleteScroll.scroll
            var suggestionHeight = _autoCompleteSuggestionItems[0].Size.Y;
            var halfOffset = Mathf.FloorToInt((_autocompleteScroll.Size.Y / suggestionHeight) * 0.5f);
            _autocompleteScroll.ScrollVertical = Mathf.FloorToInt(suggestionHeight * (_autoCompleteIndex - halfOffset));
            for (var i = 0; i < _autoCompleteSuggestionItems.Count; i++)
            {
                _autoCompleteSuggestionItems[i].IsHighlighted = i == _autoCompleteIndex;
            }
        }

        if (@event.IsAction(_inputAutoCompleteNext))
        {
            AcceptEvent();
        }
        if (@event.IsAction(_inputAutoCompletePrev))
        {
            AcceptEvent();
        }
        if (@event.IsAction("ui_cancel") && _showAutoComplete)
        {
            _showAutoComplete = false;
            AcceptEvent();
        }

        if (@event.IsAction("ui_left") && @event.IsPressed())
        {
            CallDeferred(MethodName.RefreshAutoComplete);
        }
        if (@event.IsAction("ui_right") && @event.IsPressed())
        {
            CallDeferred(MethodName.RefreshAutoComplete);
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
        ClearAutoComplete();
        _showAutoComplete = false;

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
                    var floats = SplitFloats(item.Value);
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
                    var floats = SplitFloats(item.Value);
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
                    var floats = SplitFloats(item.Value);
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
            if (methodInfo.DeclaringType!.GetCustomAttribute<GlobalClassAttribute>() is not { })
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
        _words = _commandInput.SplitCommandString();
        _showAutoComplete = _commandInput.Text.Length > 0;
        RefreshAutoComplete();
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
        SetCurrentWordIndex(int.MinValue);
        _words = [];
        _commandInput.Clear();
        _showAutoComplete = false;
        ClearAutoComplete();
        TextChanged("");
    }

    private void ClearAutoComplete()
    {
        foreach (var child in _autocompleteVbox.GetChildren())
        {
            child.QueueFree();
        }
        _autoCompleteWords = [];
        _autoCompleteIndex = 0;
        _autoCompleteSuggestionItems.Clear();
    }

    private void RefreshAutoComplete()
    {
        int wordIndex = 0;
        for (int i = 0; i < _words.Length; i++)
        {
            if (_words[i].StartIndex > _commandInput.CaretColumn) break;
            wordIndex = i;
        }

        if (_wordIndex != wordIndex)
        {
            SetCurrentWordIndex(wordIndex);
        }

        RefreshAutoCompleteValues();

        var cursorPos =
            _commandInput.GetCharacterPos(Mathf.Min(_words[wordIndex].StartIndex, _commandInput.Text.Length));
        float yOffset = 0f;
        foreach (var autoCompleteSuggestionItem in _autoCompleteSuggestionItems)
        {
            yOffset += autoCompleteSuggestionItem.Size.Y;
        }

        yOffset = Mathf.Min(yOffset, _autocompleteScroll.Size.Y);
        if (_autocompleteTooltip.Visible)
            yOffset += _autocompleteTooltip.GetHeight();
        _autocompleteControl.PivotOffset = Vector2.Zero;
        _autocompleteControl.GlobalPosition = cursorPos + Vector2.Up * yOffset;
        _autocompleteControl.PivotOffset = Vector2.Down * yOffset;
    }

    private void RefreshAutoCompleteValues()
    {
        ClearAutoComplete();

        List<SuggestionItem.SuggestionData> values = [];

        if (_wordIndex == 0 || _words.Length == 0)
        {
            foreach ((ConsoleCommandAttribute Command, MethodInfo MethodInfo) commandsValue in ConsoleData.ConsoleCommands.Values)
            {
                values.Add(new()
                {
                    Name = commandsValue.Command.Name,
                    Value = commandsValue.Command.Description
                });
            }

            _autocompleteTooltip.Visible = false;
        }
        else
        {
            if (ConsoleData.ConsoleCommands.TryGetValue(_words[0].Value, out var info))
            {
                var methodParameters = info.MethodInfo.GetParameters();
                if (_wordIndex - 1 < methodParameters.Length)
                {
                    var methodParameter = methodParameters[_wordIndex - 1];
                    var methodParameterType = Nullable.GetUnderlyingType(methodParameter.ParameterType) ??
                                        methodParameter.ParameterType;
                    var methodParameterConsoleInfo = methodParameter.GetCustomAttribute<ConsoleParamInfoAttribute>();
                    List<SuggestionItem.SuggestionData> tooltipData =
                    [
                        new("param", methodParameter.Name)
                    ];

                    if (methodParameterConsoleInfo is not null)
                    {
                        values.AddRange(GetAutocompleteValues(methodParameterConsoleInfo.AutocompleteMemberName,
                                info.MethodInfo));
                        if (methodParameterConsoleInfo.Name is { } name)
                            tooltipData[0] = tooltipData[0] with { Value = name };
                        if (methodParameterConsoleInfo.Description is { } description)
                            tooltipData.Add(new("desc", description));
                    }

                    if (Nullable.GetUnderlyingType(methodParameter.ParameterType) is { } parameterType)
                        tooltipData.Add(new("type", $"{parameterType.Name}?"));
                    else
                        tooltipData.Add(new("type", methodParameter.ParameterType.Name));
                    if (methodParameter.HasDefaultValue && methodParameter.DefaultValue is { } defaultValue)
                        tooltipData.Add(new("default", defaultValue.ToString() ?? "null"));
                    var nodePathType = methodParameter.GetCustomAttribute<NodePathTypeAttribute>();
                    if (nodePathType is not null)
                    {
                        tooltipData.Add(new("node type", nodePathType.Type.FullName!));
                    }
                    _autocompleteTooltip.Visible = true;
                    _autocompleteTooltip.SetData([..tooltipData]);

                    if (methodParameterType == typeof(bool))
                    {
                        values.AddRange([
                            new("1", "true"),
                            new("0", "false")
                        ]);
                    }
                    else if (methodParameterType.IsEnum)
                    {
                        values.AddRange(System.Enum.GetNames(methodParameterType)
                            .Select(n => new SuggestionItem.SuggestionData(n, null)));
                    }
                    else if (methodParameterType.IsAssignableTo(typeof(Node)))
                    {
                        var allPaths = GetAllChildren(
                            GetTree().Root,
                            methodParameterType
                        ).Select(n => new SuggestionItem.SuggestionData(n.GetPath().ToString(), null));
                        values.AddRange(allPaths);
                    }
                    else if (methodParameterType == typeof(NodePath))
                    {
                        var allChildren = GetAllChildren(GetTree().Root, nodePathType?.Type);
                        var allPaths = allChildren.Select(c =>
                            new SuggestionItem.SuggestionData(c.GetPath().ToString(), null));
                        values.AddRange(allPaths);
                    }
                }
                else
                {
                    _autocompleteTooltip.Visible = false;
                }
            }
        }

        if (_wordIndex >= 0 && _wordIndex < _words.Length)
            values = values.Where(w => w.Name.Contains(_words[_wordIndex].Trim('"', ' ').Value, StringComparison.InvariantCultureIgnoreCase)).ToList();

        values.Sort(((a, b) => String.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase)));

        _autoCompleteWords = values.Select(v => v.Name).ToArray();
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            var suggestionItem = _autocompleteSuggestionScene.Instantiate<SuggestionItem>();
            if (index == _autoCompleteIndex)
                suggestionItem.IsHighlighted = true;
            suggestionItem.SetData([value]);
            _autocompleteVbox.AddChild(suggestionItem);
            _autoCompleteSuggestionItems.Add(suggestionItem);
        }
    }

    public SuggestionItem.SuggestionData[] GetAutocompleteValues(string autocompleteMethodName, MethodInfo forMethod)
    {
        if (string.IsNullOrEmpty(autocompleteMethodName)) return [];

        var declaringType = forMethod.DeclaringType!;
        object result = null;
        if (declaringType.GetField(autocompleteMethodName) is { } autocompleteField)
        {
            // is field
            if (!autocompleteField.IsStatic)
            {
                PrintError($"Autocomplete field \"{autocompleteMethodName}\" is not static.");
                return [];
            }
            result = autocompleteField.GetValue(null);
        }
        else if (declaringType.GetMethod(autocompleteMethodName) is { } autocompleteMethod)
        {
            // is method
            if (!autocompleteMethod.IsStatic)
            {
                PrintError($"Autocomplete method \"{autocompleteMethodName}\" is not static.");
                return [];
            }
            result = autocompleteMethod.Invoke(null, null);
        }
        else if (declaringType.GetProperty(autocompleteMethodName) is { } autocompleteProperty)
        {
            result = autocompleteProperty.GetValue(null);
        }
        else
        {
            // not found
            PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" not found.");
            return [];
        }

        switch (result)
        {
            case SuggestionItem.SuggestionData[] resultData:
                return resultData;
            case string[] resultStrings:
                return [..resultStrings.Select(s => new SuggestionItem.SuggestionData(s, null))];
            default:
                // function does not return valid array of strings
                PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" did not return an array of strings.");
                return [];
        }
    }

    private void SetCurrentWordIndex(int index)
    {
        _wordIndex = index;
        _autocompleteControl.Scale = _autocompleteControl.Scale with
        {
            Y = 0
        };
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

    public static readonly SuggestionItem.SuggestionData[] AllScenePathsValue = [..GetFilePathsByExtension("res://", "tscn", true, ["res:///addons"])];
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

    public static List<SuggestionItem.SuggestionData> GetFilePathsByExtension(string directoryPath, string extension, bool recursive = true, string[] ignoringDirectories = null)
    {
        ignoringDirectories ??= [];
        var dir = DirAccess.Open(directoryPath);
        if (dir.ListDirBegin() != Error.Ok)
        {
            GD.PrintErr($"Could not list contents of: {directoryPath}");
            return [];
        }

        List<SuggestionItem.SuggestionData> filePaths = [];
        var thisFileName = dir.GetNext();
        while (!String.IsNullOrEmpty(thisFileName))
        {
            if (dir.CurrentIsDir() && recursive && !ignoringDirectories.Contains(dir.GetCurrentDir()))
            {
                var thisDirPath = dir.GetCurrentDir() + "/" + thisFileName;
                filePaths.AddRange(GetFilePathsByExtension(thisDirPath, extension, recursive, ignoringDirectories));
            }
            else if (thisFileName.GetExtension() == extension)
            {
                var thisFilePath = dir.GetCurrentDir() + "/" + thisFileName;
                filePaths.Add(new SuggestionItem.SuggestionData(thisFilePath.TrimPrefix("res:///"), thisFileName));
            }
            thisFileName = dir.GetNext();
        }

        return filePaths;
    }

    private static List<Node> GetAllChildren(Node node, Type type = null)
    {
        List<Node> array =
        [
            node
        ];
        foreach (var child in node.GetChildren())
        {
            array.AddRange(GetAllChildren(child));
        }

        if (type is not null)
        {
            array =
            [
                ..array.Where(
                    n => n.GetType().IsAssignableTo(type))
            ];
        }

        return array;
    }

    private static float[] SplitFloats(string instance)
    {
        var splits = instance.Split(',').Select(float.Parse).ToArray();
        return splits;
    }

    private static float ExpDecay(float a, float b, float decay, float dt)
    {
        return b + (a - b) * MathF.Exp(-decay * dt);
    }
}
