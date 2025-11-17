using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Range = System.Range;

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

    public CommandWord[] SplitCommandString() => SplitCommandString(Text);

    public static CommandWord[] SplitCommandString(string text)
    {
        if (String.IsNullOrEmpty(text)) return [new CommandWord()];

        List<CommandWord> allWords = [];
        StringBuilder substringBuilder = new StringBuilder();
        bool isInString = false;
        int wordStartIndex = 0;
        for (var index = 0; index < text.Length; index++)
        {
            var c = text[index];
            switch (c)
            {
                case '"':
                {
                    if (isInString) // finishing a substring
                    {
                        substringBuilder.Append('"');
                    }

                    if (substringBuilder.ToString().Trim().Length > 0)
                        allWords.Add(new(substringBuilder.ToString(), wordStartIndex, substringBuilder.Length));
                    substringBuilder.Clear();

                    if (!isInString) // starting a substring
                    {
                        substringBuilder.Append('"');
                        wordStartIndex = index;

                        if (index == text.Length - 1)
                        {
                            allWords.Add(new CommandWord(substringBuilder.ToString(), wordStartIndex, substringBuilder.Length));
                            substringBuilder.Clear();
                        }
                    }

                    isInString = !isInString;
                    continue;
                }
                case ' ' when !isInString:
                    if (index > 0 && text[index - 1] is not '"')
                    {
                        var newString = substringBuilder.ToString().Trim();
                        allWords.Add(new(newString, wordStartIndex, newString.Length));
                        substringBuilder.Clear();
                    }
                    wordStartIndex = index + 1;
                    if (index == text.Length - 1)
                    {
                        allWords.Add(new CommandWord("", wordStartIndex, 0));
                    }
                    continue;
            }

            if (index == text.Length - 1)
            {
                substringBuilder.Append(c);
                allWords.Add(new(substringBuilder.ToString(), wordStartIndex, substringBuilder.Length));
                substringBuilder.Clear();
                continue;
            }

            substringBuilder.Append(c);
        }

        // var allWordsFiltered = allWords.Where(s => !String.IsNullOrEmpty(s)).ToArray();
        var allWordsFiltered = allWords;

        return [..allWordsFiltered];
    }
}

public readonly record struct CommandWord(string Value, int StartIndex, int Length)
{
    public CommandWord() : this("", 0, 0) { }

    public Range Range => StartIndex..(StartIndex + Length);

    public bool IsNullOrEmpty() => String.IsNullOrEmpty(Value);
    public CommandWord Trim()
    {
        var newString = Value?.Trim();
        return this with { Value = newString, Length = newString?.Length ?? 0 };
    }

    public CommandWord Trim(params char[] characters)
    {
        var newString = Value?.Trim(characters);
        return this with { Value = newString, Length = newString?.Length ?? 0 };
    }
}
