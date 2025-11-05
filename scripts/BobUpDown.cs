using Godot;
using System;

[GlobalClass]
public partial class BobUpDown : Node
{
    [Export] public float Speed = 1f;
    [Export] public float Height = 0.25f;
    [Export] public Curve Curve;
    [Export] public float PositionInfluence = 1.0f;
    private Vector3 _initialPosition;
    private Vector3 _initialPositionGlobal;
    private Node3D _parent;
    private float _time;

    public override void _Ready()
    {
        base._Ready();
        _parent = GetParent<Node3D>();
        _initialPosition = _parent.Position;
        _initialPositionGlobal = _parent.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _time += (float)delta * Speed;
        float positionOffset = (_initialPositionGlobal.X + _initialPositionGlobal.Z + _initialPositionGlobal.Y) * PositionInfluence;
        _parent.Position = _initialPosition + Vector3.Up * Height * Curve.Sample((_time + positionOffset) % 1f);
    }
}
