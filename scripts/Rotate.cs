using Godot;
using System;

[GlobalClass]
public partial class Rotate : Node
{
    [Export] protected bool UsePhysics = false;
    [Export] public Vector3 Axis = Vector3.Up;
    [Export] public float AngleDegrees = 45f;
    [Export] public TransformSpace Space = TransformSpace.Parent;
    public float Angle
    {
        get => Mathf.DegToRad(AngleDegrees);
        set => Mathf.RadToDeg(value);
    }

    public enum TransformSpace
    {
        Local,
        Parent,
        World
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
        switch (Space)
        {
            case TransformSpace.Local:
                node.RotateObjectLocal(Axis, Angle * (float)delta);
                break;
            case TransformSpace.Parent:
                node.Rotate(Axis, Angle * (float)delta);
                break;
            case TransformSpace.World:
                node.GlobalRotate(Axis, Angle * (float)delta);
                break;
        }
    }

    public void SetAngleDegrees(float angle)
    {
        AngleDegrees = angle;
    }

    public void SetAngle(float angle)
    {
        Angle = angle;
    }
}
