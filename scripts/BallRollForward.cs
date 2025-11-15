using Godot;
using System;
using Parallas;

public partial class BallRollForward : Node
{
    [Export] private Node3D _mesh;
    [Export] private bool _useRigidbodyForward;
    public float RotateSpeed;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _mesh.RotateObjectLocal(Vector3.Right, (float)(RotateSpeed * delta));
    }
}
