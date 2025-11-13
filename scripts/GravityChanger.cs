using Godot;
using System;

public partial class GravityChanger : Node
{
    public void FlipGravity(Node3D body)
    {
        if (body is Player player)
        {
            player.FlipGravity();
        }
        else if (body is RigidBody3D rb)
        {
            rb.SetGravityScale(-rb.GravityScale);
        }
    }
}
