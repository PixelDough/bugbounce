using Godot;
using System;

[GlobalClass]
public partial class FollowParent : Node
{
    private Node3D _parent;

    public override void _Ready()
    {
        base._Ready();
        _parent = GetParent<Node3D>();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_parent is PhysicsBody3D) return;
        var nodesParent = _parent.GetParent<Node3D>();
        _parent.GlobalPosition = nodesParent.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (_parent is not PhysicsBody3D) return;
        var nodesParent = _parent.GetParent<Node3D>();
        _parent.GlobalPosition = nodesParent.GlobalPosition;
    }
}
