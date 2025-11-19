using Godot;
using System;
using System.Collections.Generic;

namespace Parallas.Commandable;
public partial class SuggestionItem : PanelContainer
{
    [Export] public Label Label;
    [Export] public Label DescriptionLabel;
    [Export] public Color FontColorBase = Colors.White;
    [Export] public Color FontColorHighlighted = Colors.Yellow;
    [Export] public StyleBox StyleBoxBase;
    [Export] public StyleBox StyleBoxHighlighted;

    private SuggestionData[] _suggestionDatas = [];
    private List<Label> _nameLabels = [];
    private List<Label> _valueLabels = [];

    public int Index = 0;

    private bool _isHighlighted = false;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value) return;
            _isHighlighted = value;
            UpdateVisual();
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        UpdateVisual();
    }

    public void SetData(SuggestionData[] suggestionDatas)
    {
        if (!IsInstanceValid(this)) return;
        for (var index = 1; index < _nameLabels.Count; index++)
        {
            var nameLabel = _nameLabels[index];
            nameLabel.QueueFree();
        }
        for (var index = 1; index < _valueLabels.Count; index++)
        {
            var valueLabel = _valueLabels[index];
            valueLabel.QueueFree();
        }

        _nameLabels.Clear();
        _valueLabels.Clear();

        _suggestionDatas = suggestionDatas;

        for (var i = 0; i < suggestionDatas.Length; i++)
        {
            var suggestion = suggestionDatas[i];

            var nameLabel = Label;
            var valueLabel = DescriptionLabel;
            if (i > 0)
            {
                nameLabel = (Label)Label.Duplicate();
                valueLabel = (Label)DescriptionLabel.Duplicate();

                Label.GetParent().AddChild(nameLabel);
                DescriptionLabel.GetParent().AddChild(valueLabel);
            }

            nameLabel.Text = suggestion.Name;
            valueLabel.Text = suggestion.Value;

            _nameLabels.Add(nameLabel);
            _valueLabels.Add(valueLabel);
        }
    }

    private void UpdateVisual()
    {
        if (!IsInstanceValid(this)) return;
        var textColor = IsHighlighted ? FontColorHighlighted : FontColorBase;
        Label.AddThemeColorOverride("font_color", textColor);
        DescriptionLabel.AddThemeColorOverride("font_color", textColor);
        AddThemeStyleboxOverride("panel", IsHighlighted ? StyleBoxHighlighted : StyleBoxBase);
        SelfModulate = Colors.White.Lerp(Colors.Black, Index % 2 * (IsHighlighted ? 0f : 0.1f));
    }

    public float GetHeight()
    {
        var height = 0f;
        Font font = null;
        foreach (var valueLabel in _valueLabels)
        {
            font ??= valueLabel.GetThemeFont("font");
            height += font.GetMultilineStringSize(
                valueLabel.Text,
                fontSize: valueLabel.GetThemeFontSize("font_size")
            ).Y;
        }
        return height;
    }

    public struct SuggestionData(string name, string value)
    {
        public string Name { get; set; } = name;
        public string Value { get; set; } = value;
    }
}
