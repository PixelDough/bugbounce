using Godot;
using System;

[GlobalClass]
public partial class Rotate : Node
{
    [Export] protected bool UsePhysics = false;
    [Export] public Vector3 Axis = Vector3.Up;
    [Export] public float AngleDegrees = 45f;
    public float Angle
    {
        get => Mathf.DegToRad(AngleDegrees);
        set => Mathf.RadToDeg(value);
    }

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

    protected virtual void DoRotation(Node3D node, double delta)
    {
        node.Rotate(Axis, Angle * (float)delta);
    }
}
