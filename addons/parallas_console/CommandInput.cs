using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class CommandInput : LineEdit
{
    private RegEx _validCharacters = new RegEx();
    public override void _Ready()
    {
        base._Ready();
        _validCharacters.Compile(@"[a-zA-Z0-9\\/""_. -]+");
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

    public Vector2 GetCaretPos()
    {
        return GetCharacterPos(CaretColumn);
    }

    public Vector2 GetCharacterPos(int index)
    {
        var leftMargin = GetThemeStylebox("normal").ContentMarginLeft;
        var scrollOffset = Mathf.FloorToInt(GetScrollOffset());

        var fontSize = GetThemeFontSize("font_size");
        var stringSize = GetThemeFont("font").GetStringSize(Text[..index], fontSize: fontSize);

        var startPos = GlobalPosition;
        return startPos + Vector2.Right * (leftMargin + stringSize.X + scrollOffset);
    }

    public string[] SplitCommandString() => SplitCommandString(Text);

    public static string[] SplitCommandString(string text)
    {
        List<string> allWords = [];
        StringBuilder substringBuilder = new StringBuilder();
        bool isInString = false;
        for (var index = 0; index < text.Length; index++)
        {
            var c = text[index];
            switch (c)
            {
                case '"':
                {
                    if (isInString) // finishing a substring
                    {
                        substringBuilder.Append(c);
                    }

                    if (substringBuilder.Length > 0)
                        allWords.Add(substringBuilder.ToString());
                    substringBuilder.Clear();

                    if (!isInString) // starting a substring
                    {
                        substringBuilder.Append(c);
                    }

                    isInString = !isInString;
                    continue;
                }
                case ' ' when !isInString:
                    allWords.Add(substringBuilder.ToString());
                    substringBuilder.Clear();
                    continue;
            }

            if (index == text.Length - 1)
            {
                substringBuilder.Append(c);
                allWords.Add(substringBuilder.ToString());
                substringBuilder.Clear();
                continue;
            }

            substringBuilder.Append(c);
        }

        var allWordsFiltered = allWords.Where(s => !String.IsNullOrEmpty(s)).ToArray();

        return [..allWordsFiltered];
    }
}
