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

    private float _offsetX = 0f;
    private int _wordIndex = int.MinValue;
    private string[] _lastInputWords = [];
    private string[] _autoCompleteWords = [];
    private readonly List<SuggestionItem> _autoCompleteSuggestionItems = [];
    private int _autoCompleteIndex = 0;
    private bool _showAutoComplete = false;
    private Tween _tween;

    private PackedScene _autocompleteSuggestionScene =
        ResourceLoader.Load<PackedScene>("res://addons/parallas_console/suggestion_item.tscn");

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
        _offsetX = -_consolePanel.Size.X;
        Position = Position with { X = _offsetX };

        _commandInput.TextChanged += TextChanged;
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

        if (!IsOpen) return;

        if (Input.IsActionJustPressed(_inputAutoCompleteConfirm))
        {
            if (!_showAutoComplete)
                _showAutoComplete = true;
            else
            {
                var lastWordLength = _lastInputWords.LastOrDefault("").Length;
                if (char.IsWhiteSpace(_commandInput.Text.LastOrDefault(' '))) lastWordLength = 0;
                var cleanedText = _commandInput.Text.Remove(_commandInput.Text.Length - lastWordLength, lastWordLength);
                _commandInput.SetText(cleanedText);
                _commandInput.CaretColumn = cleanedText.Length;
                _commandInput.InsertTextAtCaret($"{_autoCompleteWords[_autoCompleteIndex]} ");
                ClearAutoComplete();
                TextChanged(_commandInput.Text);
            }
        }

        _autocompleteControl.Visible = _showAutoComplete;
        RefreshAutoCompletePosition();
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

        var commandName = allWords[0];
        if (!ConsoleData.ConsoleCommands.TryGetValue(commandName, out var commandMethodPair))
        {
            PrintError("Invalid command provided.");
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
            var parameterType = methodParameter.ParameterType;
            var parameterDefaultValue = methodParameter.DefaultValue;

            if (Nullable.GetUnderlyingType(parameterType) is var nullableType && nullableType is not null)
                parameterType = nullableType;

            if (i < parameters.Length)
            {
                var item = parameters[i];
                if (item is null) return;

                GD.Print(parameterType.Name);
                if (parameterType == typeof(bool))
                {
                    if (bool.TryParse(item, out var boolVal))
                    {
                        parametersArray.Add(boolVal);
                    }
                    else if (item == "0" || item == "1")
                    {
                        parametersArray.Add(item == "1");
                    }
                    else
                    {
                        PrintError(
                            $"Invalid value provided for parameter \"{methodParameter.Name}\" (found \"{item}\", expected type {parameterType.Name})");
                        return;
                    }
                }
                else
                {
                    parametersArray.Add(item);
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

    public string[] GetAutocompleteValues(string autocompleteMethodName, MethodInfo forMethod)
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
        else
        {
            // not found
            PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" not found.");
            return [];
        }

        if (result is string[] resultStrings) return resultStrings;

        // function does not return valid array of strings
        PrintError($"Autocomplete method/field \"{autocompleteMethodName}\" did not return an array of strings.");
        return [];
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

    [ConsoleCommand(
        "dev_log_verbose",
        Description = "Sets whether to log console process outputs."
    )]
    public void SetVerboseLogging(bool value)
    {
        ShowVerboseLogging = value;
        GD.Print($"verbose logging set to {value}.");
    }

    private void TextChanged(string text)
    {
        _lastInputWords = _commandInput.Text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        _showAutoComplete = !_lastInputWords.IsEmpty();
        RefreshAutoCompleteValues();
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
        _wordIndex = int.MinValue;
        _lastInputWords = [];
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

    private void RefreshAutoCompletePosition()
    {
        var charCounter = 0;
        int wordIndex = 0;
        for (int i = 0; i < _lastInputWords.Length; i++)
        {
            var word = _lastInputWords[i];
            var newCharCounter = charCounter + word.Length + 1;
            if (newCharCounter > _commandInput.CaretColumn)
            {
                wordIndex = i;
                break;
            }
            charCounter = newCharCounter;
        }

        // the counting system is a bit weird. if we're on the last character show the next word position.
        if (_commandInput.CaretColumn == charCounter) wordIndex = _lastInputWords.Length;
        if (wordIndex != _wordIndex)
        {
            _autocompleteControl.GlobalPosition = _commandInput.GetCharacterPos(charCounter) + Vector2.Up * _autocompleteControl.Size.Y;
            _wordIndex = wordIndex;
            RefreshAutoCompleteValues();
            // _showAutoComplete = true;
        }
    }

    private void RefreshAutoCompleteValues()
    {
        ClearAutoComplete();

        List<string> values = [];

        if (_wordIndex == 0 || _lastInputWords.Length == 0)
        {
            values.AddRange(ConsoleData.ConsoleCommands.Keys);
        }
        else
        {
            if (ConsoleData.ConsoleCommands.TryGetValue(_lastInputWords[0], out var info))
            {
                var methodParameters = info.MethodInfo.GetParameters();
                if (_wordIndex - 1 < methodParameters.Length)
                {
                    if (methodParameters[_wordIndex - 1].ParameterType == typeof(bool))
                    {
                        values.AddRange(["true", "false"]);
                    }
                    if (methodParameters[_wordIndex - 1].ParameterType == typeof(bool?))
                    {
                        values.AddRange(["true", "false"]);
                    }
                }
                if (_wordIndex - 1 < info.Command.AutocompleteMethodNames.Length)
                {
                    values.AddRange(GetAutocompleteValues(info.Command.AutocompleteMethodNames[_wordIndex - 1],
                        info.MethodInfo));
                }
            }
        }

        if (_wordIndex >= 0 && _wordIndex < _lastInputWords.Length)
            values = values.Where(w => w.Contains(_lastInputWords[_wordIndex], StringComparison.InvariantCultureIgnoreCase)).ToList();

        values.Sort();

        _autoCompleteWords = values.ToArray();
        for (var index = values.Count - 1; index >= 0; index--)
        {
            var value = values[index];
            var suggestionItem = _autocompleteSuggestionScene.Instantiate<SuggestionItem>();
            if (index == _autoCompleteIndex)
                suggestionItem.IsHighlighted = true;
            suggestionItem.Label.Text = value;
            _autocompleteVbox.AddChild(suggestionItem);
            _autoCompleteSuggestionItems.Add(suggestionItem);
        }
        _autoCompleteSuggestionItems.Reverse();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!IsOpen) return;

        if (_autoCompleteSuggestionItems.Count > 0)
        {
            if (@event.IsActionPressed(_inputAutoCompleteNext, true))
                _autoCompleteIndex++;
            if (@event.IsActionPressed(_inputAutoCompletePrev, true))
                _autoCompleteIndex--;

            if (@event.IsEcho())
            {
                _autoCompleteIndex = Mathf.Clamp(_autoCompleteIndex, 0, _autoCompleteSuggestionItems.Count - 1);
            }
            else
            {
                if (_autoCompleteIndex >= _autoCompleteSuggestionItems.Count) _autoCompleteIndex = 0;
                if (_autoCompleteIndex < 0) _autoCompleteIndex = _autoCompleteSuggestionItems.Count - 1;
            }

            _autocompleteScroll.ScrollVertical = 10 * (_autoCompleteSuggestionItems.Count - _autoCompleteIndex - 1);
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
    }

    public static string[] DebugDrawEnumValues() => System.Enum.GetNames( typeof( Viewport.DebugDrawEnum ) );
    [ConsoleCommand(
        "debug_draw",
        AutocompleteMethodNames = [nameof(DebugDrawEnumValues)],
        CommandOutput = "Set DebugDraw on Viewport."
    )]
    public void SetDebugDraw(string debugDrawEnumString)
    {
        if (Enum.TryParse(typeof(Viewport.DebugDrawEnum), debugDrawEnumString, false, out var debugDraw) is false)
            return;
        GetViewport().SetDebugDraw((Viewport.DebugDrawEnum)debugDraw);
    }
}
