using Godot;
using System;
using System.Linq;

public partial class CommandInput : LineEdit
{
    private RegEx _validCharacters = new RegEx();
    public override void _Ready()
    {
        base._Ready();
        _validCharacters.Compile(@"[a-zA-Z0-9_. -]+");
        TextChanged += OnTextChanged;

        FocusNeighborLeft =
            FocusNeighborRight =
            FocusNeighborTop =
            FocusNeighborBottom =
            FocusNext =
            FocusPrevious =
            "."
        ;
    }

    private void OnTextChanged(string text)
    {
        var caret = CaretColumn;
        var search = _validCharacters.SearchAll(text);
        var cleanString = String.Join("", search.SelectMany(t => t.Strings));
        var oldLength = text.Length;
        Text = cleanString;
        CaretColumn = caret - (text.Length - Text.Length);
    }
}
