using Godot;
using System;

public partial class SuggestionItem : PanelContainer
{
    [Export] public Label Label;
    [Export] public Label DescriptionLabel;
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

    public void SetData(string name, string description = null)
    {
        Label.Text = name;

        DescriptionLabel.Visible = !String.IsNullOrEmpty(description);
        DescriptionLabel.Text = $": {description ?? ""}";
    }

    private void UpdateVisual()
    {
        if (!IsInstanceValid(this)) return;
        var textColor = IsHighlighted ? FontColorHighlighted : FontColorBase;
        Label.AddThemeColorOverride("font_color", textColor);
        DescriptionLabel.AddThemeColorOverride("font_color", textColor);
        AddThemeStyleboxOverride("panel", IsHighlighted ? StyleBoxHighlighted : StyleBoxBase);
    }

    public float GetHeight()
    {
        var font = GetThemeFont("font");
        var nameHeight = font.GetMultilineStringSize(
            Label.Text,
            fontSize: Label.GetThemeFontSize("font_size")
        ).Y;
        var descHeight = font.GetMultilineStringSize(
            DescriptionLabel.Text,
            fontSize: DescriptionLabel.GetThemeFontSize("font_size")
        ).Y;
        return Mathf.Max(nameHeight, descHeight);
    }
}
