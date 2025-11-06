using Godot;
using System;
using Godot.Collections;
using Parallas;

[GlobalClass]
public partial class RotateAroundParent : Rotate
{
    [Export] private Array<Node3D> _nodes = new();

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        property.Remove("space");
    }

    protected override void DoRotation(Node3D node, double delta)
    {
        foreach (var node3D in _nodes)
        {
            node3D.TranslateAround(node.GlobalPosition, MathUtil.AngleAxis(Angle * (float)delta, Axis));
        }
    }
}
