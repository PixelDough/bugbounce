using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Parallas.Commandable;
public partial class AutoComplete : Control
{
    [Export] private CommandInput _commandInput;
    [Export(PropertyHint.InputName)] private String _inputAutoCompleteConfirm;
    [Export(PropertyHint.InputName)] private String _inputAutoCompleteNext;
    [Export(PropertyHint.InputName)] private String _inputAutoCompletePrev;

    public bool IsOpen { get; private set; } = false;

    private ScrollContainer _autocompleteScroll;
    private VBoxContainer _autocompleteVbox;
    private SuggestionItem _autocompleteTooltip;

    private string[] _autoCompleteWords = [];
    private readonly List<SuggestionItem> _autoCompleteSuggestionItems = [];
    private int _autoCompleteIndex = 0;
    
    private PackedScene _autocompleteSuggestionScene =
        ResourceLoader.Load<PackedScene>("res://addons/parallas_commandable/suggestion_item.tscn");

    public override void _Ready()
    {
        base._Ready();

        _commandInput.WordIndexChanged += _ =>
        {
            Refresh();
            Reanimate();
        };

        _autocompleteScroll = GetNode<ScrollContainer>("%autocomplete_scroll");
        _autocompleteVbox = GetNode<VBoxContainer>("%autocomplete_vbox");
        _autocompleteTooltip = GetNode<SuggestionItem>("%autocomplete_tooltip");

        Clear();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!CommandableConsole.Instance.IsOpen) return;

        if (Input.IsActionJustPressed(_inputAutoCompleteConfirm))
        {
            if (!IsOpen)
            {
                Refresh();
                Open();
            }
            else
            {
                var lastWord = _commandInput.GetCurrentWord();
                var cleanedText = _commandInput.Text.Remove(lastWord.StartIndex, lastWord.Length);
                _commandInput.SetText(cleanedText);
                _commandInput.CaretColumn = lastWord.StartIndex;
                var newString = _autoCompleteWords[_autoCompleteIndex];
                if (newString.Contains(' '))
                {
                    newString = $@"""{newString}""";
                }
                _commandInput.InsertTextAtCaret(newString);
                Clear();
                _commandInput.EmitSignal(LineEdit.SignalName.TextChanged, _commandInput.Text);
            }
        }

        Modulate = Modulate with
        {
            A = CommandableUtils.ExpDecay(Modulate.A,
                IsOpen ? 1f : 0f, 50f, (float)delta)
        };
        Scale = Scale with
        {
            Y = CommandableUtils.ExpDecay(
                Scale.Y,
                IsOpen ? 1f : 0f,
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

            if (@event.IsAction(_inputAutoCompleteNext) || @event.IsAction(_inputAutoCompletePrev))
            {
                var suggestionHeight = _autoCompleteSuggestionItems[0].Size.Y;
                var halfOffset = Mathf.FloorToInt((_autocompleteScroll.Size.Y / suggestionHeight) * 0.5f);
                _autocompleteScroll.ScrollVertical =
                    Mathf.FloorToInt(suggestionHeight * (_autoCompleteIndex - halfOffset));
                for (var i = 0; i < _autoCompleteSuggestionItems.Count; i++)
                {
                    _autoCompleteSuggestionItems[i].IsHighlighted = i == _autoCompleteIndex;
                }
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
        if (@event.IsAction("ui_cancel") && IsOpen)
        {
            Close();
            AcceptEvent();
        }
    }

    public void SetIsOpen(bool state)
    {
        if (state)
            Open();
        else
            Close();
    }

    public void Open()
    {
        IsOpen = true;
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        Clear();
    }

    public void Refresh()
    {
        RefreshAutoCompleteValues();

        var cursorPos =
            _commandInput.GetCharacterPos(Mathf.Min(_commandInput.GetCurrentWord().StartIndex, _commandInput.Text.Length));
        float yOffset = 0f;
        foreach (var autoCompleteSuggestionItem in _autoCompleteSuggestionItems)
        {
            yOffset += autoCompleteSuggestionItem.Size.Y;
        }

        yOffset = Mathf.Min(yOffset, _autocompleteScroll.Size.Y);
        if (_autocompleteTooltip.Visible)
            yOffset += _autocompleteTooltip.GetHeight();
        PivotOffset = Vector2.Zero;
        GlobalPosition = cursorPos + Vector2.Up * yOffset;
        PivotOffset = Vector2.Down * yOffset;
    }

    private void RefreshAutoCompleteValues()
    {
        _autoCompleteWords = [];
        if (_autoCompleteSuggestionItems.Count > _autoCompleteIndex)
            _autoCompleteSuggestionItems[_autoCompleteIndex].IsHighlighted = false;
        _autoCompleteIndex = 0;

        List<SuggestionItem.SuggestionData> values = [];

        if (_commandInput.WordIndex == 0 || _commandInput.Words.Length == 0)
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
            if (ConsoleData.ConsoleCommands.TryGetValue(_commandInput.GetWordAtIndex(0).Value, out var info))
            {
                var methodParameters = info.MethodInfo.GetParameters();
                if (_commandInput.WordIndex - 1 < methodParameters.Length)
                {
                    var methodParameter = methodParameters[_commandInput.WordIndex - 1];
                    var methodParameterType = Nullable.GetUnderlyingType(methodParameter.ParameterType) ??
                                        methodParameter.ParameterType;
                    var methodParameterConsoleInfo = methodParameter.GetCustomAttribute<ConsoleParamInfoAttribute>();
                    List<SuggestionItem.SuggestionData> tooltipData =
                    [
                        new("param", methodParameter.Name)
                    ];

                    if (methodParameterConsoleInfo is not null)
                    {
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

                    values.AddRange(CommandableUtils.GetSuggestionsFromMember(methodParameter));
                }
                else
                {
                    _autocompleteTooltip.Visible = false;
                }
            }
        }

        if (_commandInput.WordIndex >= 0 && _commandInput.WordIndex < _commandInput.Words.Length)
            values = values.Where(w => w.Name.Contains(_commandInput.GetCurrentWord().Trim('"', ' ').Value, StringComparison.InvariantCultureIgnoreCase)).ToList();

        values.Sort(((a, b) => String.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase)));

        _autoCompleteWords = values.Select(v => v.Name).ToArray();

        if (values.Count < _autoCompleteSuggestionItems.Count)
        {
            for (int i = _autoCompleteSuggestionItems.Count - 1; i > values.Count - 1; i--)
            {
                _autoCompleteSuggestionItems[i].QueueFree();
                _autoCompleteSuggestionItems.RemoveAt(i);
            }
        }

        var countBefore = _autoCompleteSuggestionItems.Count;
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            bool alreadyExists = index < countBefore;
            var suggestionItem = alreadyExists
                ? _autoCompleteSuggestionItems[index]
                : _autocompleteSuggestionScene.Instantiate<SuggestionItem>();
            if (index == _autoCompleteIndex)
                suggestionItem.IsHighlighted = true;
            suggestionItem.Index = index;
            suggestionItem.SetData([value]);
            if (!alreadyExists)
            {
                _autocompleteVbox.AddChild(suggestionItem);
                _autoCompleteSuggestionItems.Add(suggestionItem);
            }
        }
    }

    public void Clear()
    {
        foreach (var child in _autocompleteVbox.GetChildren())
        {
            child.QueueFree();
        }
        _autoCompleteWords = [];
        _autoCompleteIndex = 0;
        _autoCompleteSuggestionItems.Clear();
    }

    public void Reanimate()
    {
        Scale = Scale with
        {
            Y = 0
        };
    }

}
