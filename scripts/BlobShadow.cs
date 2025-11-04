using Godot;
using System;
using Parallas;

[Tool]
public partial class BlobShadow : Node3D
{
    [Export] private Decal _decal;
    [Export] public float Size = 1f;
    [Export] public float Depth = 3f;
    [Export] private ShapeCast3D _shapeCast;
    [Export] private Node3D _rootOffset;

    public override void _Ready()
    {
        base._Ready();
        ProcessPriority = 100;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _decal.Size = new Vector3(Size * 1.5f, Depth, Size * 1.5f);
        _decal.Position = new Vector3(0f, 0f, Depth * 0.5f - 0.1f);
        if (!this.IsSceneRoot()) GlobalPosition = GetParent<Node3D>()?.GlobalPosition ?? Vector3.Zero;
        if (_shapeCast is not { } shapeCast) return;
        shapeCast.TargetPosition = shapeCast.TargetPosition with { Y = -100 + Size * 0.5f};
        ((SphereShape3D)shapeCast.Shape)?.SetRadius(Size * 0.5f - 0.1f);
        // ((SphereShape3D)shapeCast.Shape)?.SetRadius(0.1f);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!this.IsSceneRoot()) GlobalPosition = GetParent<Node3D>()?.GlobalPosition ?? Vector3.Zero;
        if (_shapeCast is not { } shapeCast) return;
        float finalPointDist = shapeCast.TargetPosition.Y * shapeCast.TargetPosition.Y;
        for (int i = 0; i < shapeCast.GetCollisionCount(); i++)
        {
            if (Mathf.Abs(shapeCast.GetCollisionNormal(i).Dot(Vector3.Up)) < 0.01f) continue;
            var point = shapeCast.GetCollisionPoint(i);
            float newDist = point.DistanceSquaredTo(shapeCast.GlobalPosition);
            if (newDist < finalPointDist)
            {
                finalPointDist = newDist;
                _rootOffset.GlobalPosition = _rootOffset.GlobalPosition with { Y = point.Y };
            }
        }
    }
}
