using Godot;
using System;
using Parallas;

[GlobalClass]
public partial class Billboard : Node
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        var parent = GetParent<Node3D>();
        if (parent is null) return;

        var dir = ((parent.GlobalPosition - GetViewport().GetCamera3D().GlobalPosition) with { Y = 0 }).Normalized();
        var up = GetViewport().GetCamera3D().GlobalBasis.Y;
        parent.GlobalRotation = MathUtil.LookRotation(dir, Vector3.Up).GetEuler();
    }
}
