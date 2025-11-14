using Godot;
using System;

public partial class SuggestionItem : PanelContainer
{
    [Export] public Label Label;
    [Export] public Color FontColorBase = Colors.White;
    [Export] public Color FontColorHighlighted = Colors.Yellow;
    [Export] public StyleBox StyleBoxBase;
    [Export] public StyleBox StyleBoxHighlighted;

    private bool _isHighlighted = false;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            _isHighlighted = value;
            UpdateVisual();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (!IsInstanceValid(this)) return;
        Label.AddThemeColorOverride("font_color", IsHighlighted ? FontColorHighlighted : FontColorBase);
        AddThemeStyleboxOverride("panel", IsHighlighted ? StyleBoxHighlighted : StyleBoxBase);
    }
}
