using Godot;
using System;
using Godot.Collections;
using Parallas;

[GlobalClass, Tool]
public partial class RotateAroundParent : Rotate
{
    [Export] private Array<Node3D> _nodes = new();

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].As<String>() == "Space")
        {
            property["usage"] = PropertyUsageFlags.NoEditor.ToString();
        }
    }

    protected override void DoRotation(Node3D node, double delta)
    {
        if (Engine.IsEditorHint()) return;
        foreach (var node3D in _nodes)
        {
            node3D.TranslateAround(node.GlobalPosition, MathUtil.AngleAxis(Angle * (float)delta, Axis));
        }
    }
}
