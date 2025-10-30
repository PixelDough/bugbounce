using Godot;
using System;

[GlobalClass]
public partial class RotateWobble : Node
{
    private float _time;

    private Node3D _parent3d;

    public override void _Ready()
    {
        base._Ready();
        _parent3d = GetParent<Node3D>();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _time += (float)delta;
        _parent3d.RotationDegrees = new Vector3(Mathf.Sin(_time * 2f) * 15, -Mathf.Cos(_time * 2f) * 15, 0f);
    }
}
