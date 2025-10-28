using Godot;
using System;
using Godot.Collections;
using Parallas;

[GlobalClass]
public partial class RotateAroundParent : Rotate
{
    [Export] private Array<Node3D> _nodes = new();
    public override void _Process(double delta)
    {
        if (UsePhysics) return;
        if (GetParent() is not Node3D parent) return;
        DoRotation(parent, delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!UsePhysics) return;
        if (GetParent() is not Node3D parent) return;
        DoRotation(parent, delta);
    }

    protected override void DoRotation(Node3D node, double delta)
    {
        foreach (var node3D in _nodes)
        {
            node3D.TranslateAround(node.GlobalPosition, MathUtil.AngleAxis(Angle * (float)delta, Axis));
        }
    }
}
