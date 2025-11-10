using Godot;
using System;

public partial class GravityChanger : Node
{
    public void FlipGravity(Node3D body)
    {
        if (body is not RigidBody3D rb) return;
        rb.SetGravityScale(-rb.GravityScale);
    }
}
